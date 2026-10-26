using Depot.Domain.Common;

namespace Depot.Domain.ReleaseOrders;

/// <summary>How many containers of one type and size a release order allows to leave the depot (BR-RO-02).</summary>
public sealed class ReleaseOrderItem : AuditableEntity
{
    internal ReleaseOrderItem(Guid releaseOrderId, Guid containerTypeId, int sizeFeet, int quantity)
    {
        ReleaseOrderId = releaseOrderId;
        ContainerTypeId = containerTypeId;
        SizeFeet = sizeFeet;
        Quantity = quantity;
    }

    public Guid ReleaseOrderId { get; private set; }

    public Guid ContainerTypeId { get; private set; }

    public int SizeFeet { get; private set; }

    public int Quantity { get; private set; }
}
