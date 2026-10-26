namespace Depot.Domain.Gate;

/// <summary>The vehicle that brought a container in; a value object stored inside the stay (A-12).</summary>
public sealed record Vehicle(VehicleType Type, string? Number);
