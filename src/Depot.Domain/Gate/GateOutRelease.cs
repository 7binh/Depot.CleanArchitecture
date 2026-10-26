using Depot.Domain.ReleaseOrders;

namespace Depot.Domain.Gate;

/// <summary>
/// The release order a container leaves on, with the count the rules need. Application loads it; the Domain never queries.
/// <paramref name="DeliveredOnLine"/> is how many containers already left on the order line that matches this container
/// (see <see cref="ReleaseOrder.FindItemFor"/>); pass 0 when no line matches.
/// </summary>
public sealed record GateOutRelease(ReleaseOrder Order, int DeliveredOnLine);
