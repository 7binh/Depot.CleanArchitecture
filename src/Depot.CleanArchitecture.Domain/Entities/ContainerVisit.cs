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

    private readonly List<ConditionSurvey> _surveys = [];
    public IReadOnlyCollection<ConditionSurvey> Surveys => _surveys.AsReadOnly();

    private readonly List<StayHold> _holds = [];
    public IReadOnlyCollection<StayHold> Holds => _holds.AsReadOnly();

    private readonly List<MovementEvent> _movements = [];
    public IReadOnlyCollection<MovementEvent> Movements => _movements.AsReadOnly();

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
        CurrentBlockId = null; // Rời khỏi khu vực bãi
    }

    public void Complete()
    {
        Status = ContainerVisitStatus.Completed;
    }

    public StayHold? AddSurvey(ConditionSurvey survey)
    {
        _surveys.Add(survey);
        InSurveyGrade = survey.Grade;

        if (survey.Grade is ContainerGrade.GradeD_Damaged or ContainerGrade.GradeE_Scrap)
        {
            var holdResult = ApplyHold("DAMAGE_HOLD", $"Tự động khóa giữ do giám định vỏ xếp loại {survey.Grade}: {survey.Notes}", survey.InspectedBy);
            return holdResult.Value;
        }

        return null;
    }

    public Result<StayHold> ApplyHold(string holdCode, string reason, string heldBy)
    {
        var holdResult = StayHold.Create(TenantId, Id, holdCode, reason, heldBy);
        if (holdResult.IsFailure) return holdResult;

        _holds.Add(holdResult.Value!);
        return holdResult;
    }

    public Result ReleaseHold(Guid holdId, string releaseReason, string releasedBy)
    {
        var hold = _holds.FirstOrDefault(h => h.Id == holdId);
        if (hold is null)
            return Result.Failure(new Error("Hold.NotFound", "Không tìm thấy lệnh giữ này trên container.", ErrorType.NotFound));

        return hold.Release(releaseReason, releasedBy);
    }

    public void Relocate(Guid? newSlotId, Guid? newBlockId, string actorId, string? reason)
    {
        var fromSlotId = CurrentSlotId;
        CurrentSlotId = newSlotId;
        CurrentBlockId = newBlockId;
        Status = ContainerVisitStatus.StackedInYard;

        var eventType = newSlotId.HasValue ? "RELOCATE_YARD" : "MOVE_TO_VIRTUAL";
        _movements.Add(MovementEvent.Create(TenantId, Id, eventType, fromSlotId, newSlotId, actorId, reason));
    }

    public void AddMovement(MovementEvent movement)
    {
        _movements.Add(movement);
    }
}
