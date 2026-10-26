namespace Depot.Domain.Gate;

/// <summary>What the gate clerk records about the arrival, grouped so that <see cref="ContainerStay.GateIn"/> stays short.</summary>
public sealed record GateInDetails(
    DateTimeOffset GateInAt,
    StayGrade Grade,
    StayCondition Condition,
    string? DamageNote,
    Vehicle InVehicle);
