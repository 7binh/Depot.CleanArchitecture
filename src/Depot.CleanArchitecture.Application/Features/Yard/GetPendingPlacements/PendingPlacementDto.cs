namespace Depot.CleanArchitecture.Application.Features.Yard.GetPendingPlacements;

public sealed record PendingPlacementDto(
    Guid VisitId,
    Guid ContainerId,
    string ContainerNumber,
    string LineOperator,
    int Size,
    string Type,
    string IsoCode,
    string Grade,
    string GradeDescription,
    bool IsReefer,
    DateTime GateInDate,
    int WaitingMinutes,
    string TractorNo,
    string TrailerNo,
    string DriverName,
    string? DriverPhone,
    string? DamageNotes);
