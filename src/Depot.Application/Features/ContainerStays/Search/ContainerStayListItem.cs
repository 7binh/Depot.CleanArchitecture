using Depot.Domain.Gate;

namespace Depot.Application.Features.ContainerStays.Search;

/// <summary>A stay with the number and operator of its container, so a list needs no second request.</summary>
public sealed record ContainerStayListItem(
    Guid Id,
    Guid ContainerId,
    string ContainerNumber,
    string LineOperatorCode,
    DateTimeOffset GateInAt,
    StayGrade Grade,
    StayCondition Condition,
    string? DamageNote,
    VehicleType InVehicleType,
    string InVehicleNumber,
    Guid BlockId,
    Guid? RowId,
    int? Tier,
    DateTimeOffset? GateOutAt,
    VehicleType? OutVehicleType,
    string? OutVehicleNumber,
    Guid? ReleaseOrderItemId);
