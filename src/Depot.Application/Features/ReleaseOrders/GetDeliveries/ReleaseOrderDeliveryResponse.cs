using Depot.Domain.Gate;

namespace Depot.Application.Features.ReleaseOrders.GetDeliveries;

/// <summary>One container that has left the depot on a release order, with the line it counts against.</summary>
public sealed record ReleaseOrderDeliveryResponse(
    Guid StayId,
    Guid ContainerId,
    string ContainerNumber,
    Guid ContainerTypeId,
    int SizeFeet,
    DateTimeOffset GateOutAt,
    VehicleType? OutVehicleType,
    string? OutVehicleNumber);
