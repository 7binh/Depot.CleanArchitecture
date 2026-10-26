using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders.GetById;

public sealed class GetReleaseOrderByIdHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetReleaseOrderByIdQuery, Result<ReleaseOrderResponse>>
{
    public async Task<Result<ReleaseOrderResponse>> HandleAsync(
        GetReleaseOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ReleaseOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure<ReleaseOrderResponse>(ReleaseOrderErrors.ReleaseOrderNotFound(query.Id));
        }

        var itemIds = order.Items.Select(x => x.Id).ToList();
        var delivered = await DeliveredCounts.LoadAsync(dbContext, itemIds, cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        return Result.Success(ReleaseOrderResponse.From(order, today, delivered));
    }
}
