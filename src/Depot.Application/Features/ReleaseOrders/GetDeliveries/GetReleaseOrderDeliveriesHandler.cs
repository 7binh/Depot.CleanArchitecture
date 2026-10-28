using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders.GetDeliveries;

public sealed class GetReleaseOrderDeliveriesHandler(IAppDbContext dbContext)
    : IQueryHandler<GetReleaseOrderDeliveriesQuery, Result<IReadOnlyList<ReleaseOrderDeliveryResponse>>>
{
    public async Task<Result<IReadOnlyList<ReleaseOrderDeliveryResponse>>> HandleAsync(
        GetReleaseOrderDeliveriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.ReleaseOrders.AnyAsync(x => x.Id == query.Id, cancellationToken);
        if (!exists)
        {
            return Result.Failure<IReadOnlyList<ReleaseOrderDeliveryResponse>>(
                ReleaseOrderErrors.ReleaseOrderNotFound(query.Id));
        }

        IReadOnlyList<ReleaseOrderDeliveryResponse> deliveries = await dbContext.ReleaseOrders
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .SelectMany(x => x.Items)
            .Join(
                dbContext.ContainerStays.Where(s => s.GateOutAt != null),
                item => (Guid?)item.Id,
                stay => stay.ReleaseOrderItemId,
                (item, stay) => new { Item = item, Stay = stay })
            .Join(
                dbContext.Containers,
                x => x.Stay.ContainerId,
                container => container.Id,
                (x, container) => new { x.Item, x.Stay, Container = container })
            .OrderByDescending(x => x.Stay.GateOutAt)
            .Select(x => new ReleaseOrderDeliveryResponse(
                x.Stay.Id,
                x.Container.Id,
                x.Container.Number,
                x.Item.ContainerTypeId,
                x.Item.SizeFeet,
                x.Stay.GateOutAt!.Value,
                x.Stay.OutVehicleType,
                x.Stay.OutVehicleNumber))
            .ToListAsync(cancellationToken);

        return Result.Success(deliveries);
    }
}
