using Depot.Domain.ReleaseOrders;

namespace Depot.Application.Features.ReleaseOrders;

public sealed record ReleaseOrderItemResponse(
    Guid ContainerTypeId,
    int SizeFeet,
    int Quantity,
    int Delivered,
    int Remaining);

public sealed record ReleaseOrderResponse(
    Guid Id,
    Guid LineOperatorId,
    Guid CustomerId,
    string OrderNo,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    string ExportVessel,
    bool IsExpired,
    ReleaseOrderStatus Status,
    IReadOnlyList<ReleaseOrderItemResponse> Items)
{
    internal static ReleaseOrderResponse From(
        ReleaseOrder order,
        DateOnly today,
        IReadOnlyDictionary<Guid, int> delivered)
    {
        var items = order.Items
            .OrderBy(x => x.SizeFeet)
            .Select(x => ItemFrom(x, delivered.GetValueOrDefault(x.Id)))
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
            StatusOf(items),
            items);
    }

    private static ReleaseOrderItemResponse ItemFrom(ReleaseOrderItem item, int delivered)
    {
        return new ReleaseOrderItemResponse(
            item.ContainerTypeId,
            item.SizeFeet,
            item.Quantity,
            delivered,
            Math.Max(0, item.Quantity - delivered));
    }

    private static ReleaseOrderStatus StatusOf(IReadOnlyList<ReleaseOrderItemResponse> items)
    {
        if (items.All(x => x.Remaining == 0))
        {
            return ReleaseOrderStatus.Delivered;
        }

        return items.Any(x => x.Delivered > 0)
            ? ReleaseOrderStatus.Delivering
            : ReleaseOrderStatus.NotDelivered;
    }
}
