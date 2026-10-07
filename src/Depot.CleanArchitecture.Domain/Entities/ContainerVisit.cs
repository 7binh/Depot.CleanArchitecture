namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;

public class ContainerVisit : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ContainerId { get; private set; }
    public string LineOperator { get; private set; } = null!;
    public ContainerVisitStatus Status { get; private set; }
    public Guid? CurrentSlotId { get; private set; }
    public Guid? CurrentBlockId { get; private set; }

    // Thông tin Nhập bãi (Gate-In)
    public DateTime GateInDate { get; private set; }
    public VehicleInfo InVehicle { get; private set; } = null!;
    public ContainerGrade InSurveyGrade { get; private set; }
    public string? InDamageNotes { get; private set; }

    // Thông tin Xuất bãi (Gate-Out)
    public DateTime? GateOutDate { get; private set; }
    public VehicleInfo? OutVehicle { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    private ContainerVisit() { }

    public static ContainerVisit CreateGateIn(
        Guid tenantId,
        Guid containerId,
        string lineOperator,
        VehicleInfo vehicle,
        ContainerGrade surveyGrade,
        string? damageNotes)
    {
        return new ContainerVisit
        {
            TenantId = tenantId,
            ContainerId = containerId,
            LineOperator = lineOperator.ToUpperInvariant(),
            Status = ContainerVisitStatus.GatedIn,
            GateInDate = DateTime.UtcNow,
            InVehicle = vehicle,
            InSurveyGrade = surveyGrade,
            InDamageNotes = damageNotes
        };
    }

    public void AssignSlot(Guid slotId, Guid blockId)
    {
        CurrentSlotId = slotId;
        CurrentBlockId = blockId;
        Status = ContainerVisitStatus.StackedInYard;
    }

    public void AssignVirtualBlock(Guid virtualBlockId)
    {
        CurrentSlotId = null;
        CurrentBlockId = virtualBlockId;
        Status = ContainerVisitStatus.StackedInYard;
    }

    public void AllocateToOrder(Guid deliveryOrderId)
    {
        DeliveryOrderId = deliveryOrderId;
        Status = ContainerVisitStatus.Allocated;
    }

    public void GateOut(VehicleInfo outVehicle)
    {
        OutVehicle = outVehicle;
        GateOutDate = DateTime.UtcNow;
        Status = ContainerVisitStatus.GatedOut;
        CurrentSlotId = null; // Giải phóng ô bãi
    }

    public void Complete()
    {
        Status = ContainerVisitStatus.Completed;
    }
}
