using Depot.Domain.Gate;

namespace Depot.Application.Features.ContainerStays.GateIn;

/// <summary>
/// Raw input for gating a container in (BR-GI-02). <c>GateInAt</c> defaults to now when empty;
/// <c>RowId</c> and <c>Tier</c> stay empty for a virtual block.
/// </summary>
public sealed record GateInCommand(
    Guid ContainerId,
    DateTimeOffset? GateInAt,
    StayGrade Grade,
    StayCondition Condition,
    string? DamageNote,
    VehicleType InVehicleType,
    string? InVehicleNumber,
    Guid BlockId,
    Guid? RowId,
    int? Tier);
