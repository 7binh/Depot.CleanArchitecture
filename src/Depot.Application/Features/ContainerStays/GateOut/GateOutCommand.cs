using Depot.Domain.Gate;

namespace Depot.Application.Features.ContainerStays.GateOut;

/// <summary>
/// Raw input for letting a container leave on a release order (BR-GO-02). The stay is the container's open one.
/// <c>GateOutAt</c> defaults to now when empty.
/// </summary>
public sealed record GateOutCommand(
    Guid ContainerId,
    Guid ReleaseOrderId,
    DateTimeOffset? GateOutAt,
    VehicleType OutVehicleType,
    string? OutVehicleNumber);
