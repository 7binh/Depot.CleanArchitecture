using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.ReleaseOrders;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders.Search;

public sealed class SearchReleaseOrdersHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<SearchReleaseOrdersQuery, Result<PagedResult<ReleaseOrderResponse>>>
{
    private const string LikeEscape = "\\";

    public async Task<Result<PagedResult<ReleaseOrderResponse>>> HandleAsync(
        SearchReleaseOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, SearchReleaseOrdersQuery.MaxPageSize);

        var filtered = Filter(dbContext.ReleaseOrders.AsNoTracking(), query, today);
        var total = await filtered.CountAsync(cancellationToken);

        var orders = await filtered
            .Include(x => x.Items)
            .OrderByDescending(x => x.IssuedDate)
            .ThenBy(x => x.OrderNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var itemIds = orders.SelectMany(x => x.Items).Select(x => x.Id).ToList();
        var delivered = await DeliveredCounts.LoadAsync(dbContext, itemIds, cancellationToken);
        var items = orders.Select(x => ReleaseOrderResponse.From(x, today, delivered)).ToList();

        return Result.Success(new PagedResult<ReleaseOrderResponse>(items, total, page, pageSize));
    }

    private IQueryable<ReleaseOrder> Filter(IQueryable<ReleaseOrder> source, SearchReleaseOrdersQuery query, DateOnly today)
    {
        if (query.LineOperatorId is { } operatorId)
        {
            source = source.Where(x => x.LineOperatorId == operatorId);
        }

        if (query.OpenOnly)
        {
            // A line has room while fewer containers have left on it than the order allows (BR-RO-06).
            source = source.Where(x => x.ExpiryDate >= today
                && x.Items.Any(i => i.Quantity > dbContext.ContainerStays.Count(s => s.ReleaseOrderItemId == i.Id)));
        }

        if (query.Status is { } status)
        {
            source = FilterByStatus(source, status, today);
        }

        return string.IsNullOrWhiteSpace(query.OrderNo) ? source : FilterByOrderNo(source, query.OrderNo.Trim());
    }

    private IQueryable<ReleaseOrder> FilterByStatus(IQueryable<ReleaseOrder> source, ReleaseOrderListStatus status, DateOnly today)
    {
        return status switch
        {
            ReleaseOrderListStatus.Delivered => source.Where(x => x.Items.All(i =>
                i.Quantity <= dbContext.ContainerStays.Count(s => s.ReleaseOrderItemId == i.Id))),
            ReleaseOrderListStatus.Expired => source.Where(x => x.ExpiryDate < today && x.Items.Any(i =>
                i.Quantity > dbContext.ContainerStays.Count(s => s.ReleaseOrderItemId == i.Id))),
            ReleaseOrderListStatus.Delivering => source.Where(x => x.ExpiryDate >= today
                && x.Items.Any(i => i.Quantity > dbContext.ContainerStays.Count(s => s.ReleaseOrderItemId == i.Id))
                && x.Items.Any(i => dbContext.ContainerStays.Any(s => s.ReleaseOrderItemId == i.Id))),
            _ => source.Where(x => x.ExpiryDate >= today
                && !x.Items.Any(i => dbContext.ContainerStays.Any(s => s.ReleaseOrderItemId == i.Id))),
        };
    }

    // Order numbers keep the case they were entered in, and a LIKE on PostgreSQL is case-sensitive, so the
    // text is tried as typed, upper-case and lower-case instead of changing the case of the column.
    private static IQueryable<ReleaseOrder> FilterByOrderNo(IQueryable<ReleaseOrder> source, string text)
    {
        var typed = $"%{EscapeLike(text)}%";
        var upper = $"%{EscapeLike(text.ToUpperInvariant())}%";
        var lower = $"%{EscapeLike(text.ToLowerInvariant())}%";
        return source.Where(x => EF.Functions.Like(x.OrderNo, typed, LikeEscape)
            || EF.Functions.Like(x.OrderNo, upper, LikeEscape)
            || EF.Functions.Like(x.OrderNo, lower, LikeEscape));
    }

    private static string EscapeLike(string text)
    {
        return text
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
    }
}
