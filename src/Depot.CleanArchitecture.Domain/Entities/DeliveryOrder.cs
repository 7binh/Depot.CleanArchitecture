namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public class DeliveryOrder : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; private set; } = null!;
    public string LineOperator { get; private set; } = null!;
    public string CustomerName { get; private set; } = null!;
    public string CustomerTaxCode { get; private set; } = null!;
    public DateOnly ExpirationDate { get; private set; }
    public string VesselName { get; private set; } = null!;
    public string VoyageNo { get; private set; } = null!;
    public DeliveryOrderStatus Status { get; private set; }

    private readonly List<DeliveryOrderItem> _items = new();
    public IReadOnlyCollection<DeliveryOrderItem> Items => _items.AsReadOnly();

    private DeliveryOrder() { }

    public static DeliveryOrder Create(
        Guid tenantId,
        string orderNumber,
        string lineOperator,
        string customerName,
        string customerTaxCode,
        DateOnly expirationDate,
        string vesselName,
        string voyageNo)
    {
        return new DeliveryOrder
        {
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            CustomerName = customerName.Trim(),
            CustomerTaxCode = customerTaxCode.Trim(),
            ExpirationDate = expirationDate,
            VesselName = vesselName.Trim().ToUpperInvariant(),
            VoyageNo = voyageNo.Trim().ToUpperInvariant(),
            Status = DeliveryOrderStatus.Active
        };
    }

    public void AddItem(int size, ContainerType type, ContainerGrade requiredGrade, int quantity)
    {
        _items.Add(new DeliveryOrderItem(Id, size, type, requiredGrade, quantity));
    }

    public void ExtendExpirationDate(DateOnly newExpirationDate)
    {
        if (newExpirationDate > ExpirationDate)
        {
            ExpirationDate = newExpirationDate;
            if (Status == DeliveryOrderStatus.Expired)
                Status = DeliveryOrderStatus.Active;
        }
    }
}
