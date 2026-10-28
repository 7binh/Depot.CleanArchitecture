namespace Depot.Application.Features.ReleaseOrders.Search;

/// <summary>The status a list shows for an order: the delivery status, or Expired while an order past its date still had room.</summary>
public enum ReleaseOrderListStatus
{
    NotDelivered,
    Delivering,
    Delivered,
    Expired,
}
