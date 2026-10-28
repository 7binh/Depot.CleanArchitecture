using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays.Search;

public sealed class SearchContainerStaysHandler(IAppDbContext dbContext)
    : IQueryHandler<SearchContainerStaysQuery, Result<PagedResult<ContainerStayListItem>>>
{
    public async Task<Result<PagedResult<ContainerStayListItem>>> HandleAsync(
        SearchContainerStaysQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, SearchContainerStaysQuery.MaxPageSize);

        var filtered = Filter(dbContext.ContainerStays.AsNoTracking(), query);
        var total = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .Join(dbContext.Containers, s => s.ContainerId, c => c.Id, (s, c) => new { Stay = s, Container = c })
            .Join(dbContext.LineOperators, x => x.Container.LineOperatorId, o => o.Id, (x, o) => new { x.Stay, x.Container, Operator = o })
            // Sort and cut the page before building the record: a database cannot sort by a record's property.
            .OrderByDescending(x => x.Stay.GateInAt)
            .ThenBy(x => x.Stay.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ContainerStayListItem(
                x.Stay.Id,
                x.Stay.ContainerId,
                x.Container.Number,
                x.Operator.Code,
                x.Stay.GateInAt,
                x.Stay.Grade,
                x.Stay.Condition,
                x.Stay.DamageNote,
                x.Stay.InVehicleType,
                x.Stay.InVehicleNumber,
                x.Stay.BlockId,
                x.Stay.RowId,
                x.Stay.Tier,
                x.Stay.GateOutAt,
                x.Stay.OutVehicleType,
                x.Stay.OutVehicleNumber,
                x.Stay.ReleaseOrderItemId))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ContainerStayListItem>(items, total, page, pageSize));
    }

    private IQueryable<ContainerStay> Filter(IQueryable<ContainerStay> source, SearchContainerStaysQuery query)
    {
        if (query.InYard is { } inYard)
        {
            source = inYard
                ? source.Where(x => x.GateOutAt == null)
                : source.Where(x => x.GateOutAt != null);
        }

        // Numbers are stored upper-case, so the search text is upper-cased instead of the column.
        if (!string.IsNullOrWhiteSpace(query.Number))
        {
            var number = query.Number.Trim().ToUpperInvariant();
            source = source.Where(x => dbContext.Containers.Any(c => c.Id == x.ContainerId && c.Number.Contains(number)));
        }

        return source;
    }
}
