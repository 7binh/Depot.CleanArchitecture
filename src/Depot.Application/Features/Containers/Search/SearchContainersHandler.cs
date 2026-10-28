using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Containers.Search;

public sealed class SearchContainersHandler(IAppDbContext dbContext)
    : IQueryHandler<SearchContainersQuery, Result<PagedResult<ContainerListItem>>>
{
    public async Task<Result<PagedResult<ContainerListItem>>> HandleAsync(
        SearchContainersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, SearchContainersQuery.MaxPageSize);

        var filtered = Filter(dbContext.Containers.AsNoTracking(), query);
        var total = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .Join(dbContext.LineOperators, x => x.LineOperatorId, o => o.Id, (x, o) => new { Container = x, Operator = o })
            .Join(dbContext.ContainerTypes, x => x.Container.ContainerTypeId, t => t.Id, (x, t) => new { x.Container, x.Operator, Type = t })
            // Sort and cut the page before building the record: a database cannot sort by a record's property.
            .OrderBy(x => x.Container.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ContainerListItem(
                x.Container.Id,
                x.Container.Number,
                x.Operator.Id,
                x.Operator.Code,
                x.Type.Id,
                x.Type.Code,
                x.Container.IsoCode,
                x.Container.SizeFeet,
                x.Container.MaxWeightKg,
                x.Container.TareWeightKg,
                x.Container.ManufactureDate))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ContainerListItem>(items, total, page, pageSize));
    }

    private IQueryable<Container> Filter(IQueryable<Container> source, SearchContainersQuery query)
    {
        // Numbers and ISO codes are stored upper-case, so the search text is upper-cased instead of the column.
        if (!string.IsNullOrWhiteSpace(query.Number))
        {
            var number = query.Number.Trim().ToUpperInvariant();
            source = source.Where(x => x.Number.Contains(number));
        }

        if (!string.IsNullOrWhiteSpace(query.IsoCode))
        {
            var isoCode = query.IsoCode.Trim().ToUpperInvariant();
            source = source.Where(x => x.IsoCode.Contains(isoCode));
        }

        if (query.LineOperatorId is { } operatorId)
        {
            source = source.Where(x => x.LineOperatorId == operatorId);
        }

        if (query.ContainerTypeId is { } typeId)
        {
            source = source.Where(x => x.ContainerTypeId == typeId);
        }

        if (query.SizeFeet is { } sizeFeet)
        {
            source = source.Where(x => x.SizeFeet == sizeFeet);
        }

        if (query.InYard is { } inYard)
        {
            source = source.Where(x => dbContext.ContainerStays
                .Any(s => s.ContainerId == x.Id && s.GateOutAt == null) == inYard);
        }

        return source;
    }
}
