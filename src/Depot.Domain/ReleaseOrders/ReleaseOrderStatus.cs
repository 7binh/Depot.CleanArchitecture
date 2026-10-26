namespace Depot.Domain.ReleaseOrders;

/// <summary>How far a release order has been delivered; worked out from the gate outs, never stored.</summary>
public enum ReleaseOrderStatus
{
    NotDelivered,
    Delivering,
    Delivered
}
