namespace Depot.Domain.Gate;

/// <summary>What the gate clerk records about the departure (BR-GO-02, BR-GO-10).</summary>
public sealed record GateOutDetails(DateTimeOffset GateOutAt, Vehicle OutVehicle);
