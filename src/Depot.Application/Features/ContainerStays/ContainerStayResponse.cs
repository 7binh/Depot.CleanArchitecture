using Depot.Domain.Gate;

namespace Depot.Application.Features.ContainerStays;

public sealed record ContainerStayResponse(
    Guid Id,
    Guid ContainerId,
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
    Guid? ReleaseOrderItemId)
{
    internal static ContainerStayResponse From(ContainerStay stay)
    {
        return new ContainerStayResponse(
            stay.Id,
            stay.ContainerId,
            stay.GateInAt,
            stay.Grade,
            stay.Condition,
            stay.DamageNote,
            stay.InVehicleType,
            stay.InVehicleNumber,
            stay.BlockId,
            stay.RowId,
            stay.Tier,
            stay.GateOutAt,
            stay.OutVehicleType,
            stay.OutVehicleNumber,
            stay.ReleaseOrderItemId);
    }
}
