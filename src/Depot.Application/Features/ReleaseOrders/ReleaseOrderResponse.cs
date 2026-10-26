using Depot.Domain.ReleaseOrders;

namespace Depot.Application.Features.ReleaseOrders;

public sealed record ReleaseOrderItemResponse(Guid ContainerTypeId, int SizeFeet, int Quantity);

public sealed record ReleaseOrderResponse(
    Guid Id,
    Guid LineOperatorId,
    Guid CustomerId,
    string OrderNo,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    string ExportVessel,
    bool IsExpired,
    IReadOnlyList<ReleaseOrderItemResponse> Items)
{
    internal static ReleaseOrderResponse From(ReleaseOrder order, DateOnly today)
    {
        var items = order.Items
            .OrderBy(x => x.SizeFeet)
            .Select(x => new ReleaseOrderItemResponse(x.ContainerTypeId, x.SizeFeet, x.Quantity))
            .ToList();

        return new ReleaseOrderResponse(
            order.Id,
            order.LineOperatorId,
            order.CustomerId,
            order.OrderNo,
            order.IssuedDate,
            order.ExpiryDate,
            order.ExportVessel,
            order.IsExpiredOn(today),
            items);
    }
}
