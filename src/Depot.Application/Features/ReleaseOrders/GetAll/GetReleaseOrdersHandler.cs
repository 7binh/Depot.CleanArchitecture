using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders.GetAll;

public sealed class GetReleaseOrdersHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetReleaseOrdersQuery, Result<IReadOnlyList<ReleaseOrderResponse>>>
{
    public async Task<Result<IReadOnlyList<ReleaseOrderResponse>>> HandleAsync(
        GetReleaseOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        var orders = await dbContext.ReleaseOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.IssuedDate)
            .ThenBy(x => x.OrderNo)
            .ToListAsync(cancellationToken);

        var itemIds = orders.SelectMany(x => x.Items).Select(x => x.Id).ToList();
        var delivered = await DeliveredCounts.LoadAsync(dbContext, itemIds, cancellationToken);

        IReadOnlyList<ReleaseOrderResponse> responses = orders
            .Select(x => ReleaseOrderResponse.From(x, today, delivered))
            .ToList();

        return Result.Success(responses);
    }
}
