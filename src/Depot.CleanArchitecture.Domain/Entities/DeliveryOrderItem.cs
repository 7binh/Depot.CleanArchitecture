namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public class DeliveryOrderItem : BaseEntity
{
    public Guid DeliveryOrderId { get; private set; }
    public int Size { get; private set; }
    public ContainerType Type { get; private set; }
    public ContainerGrade RequiredGrade { get; private set; }
    public int OrderedQuantity { get; private set; }
    public int DeliveredQuantity { get; private set; }

    internal DeliveryOrderItem(Guid orderId, int size, ContainerType type, ContainerGrade grade, int orderedQty)
    {
        DeliveryOrderId = orderId;
        Size = size;
        Type = type;
        RequiredGrade = grade;
        OrderedQuantity = orderedQty;
        DeliveredQuantity = 0;
    }

    private DeliveryOrderItem() { } // Dành cho EF Core

    public void RecordDelivery()
    {
        if (DeliveredQuantity < OrderedQuantity)
            DeliveredQuantity++;
    }
}
