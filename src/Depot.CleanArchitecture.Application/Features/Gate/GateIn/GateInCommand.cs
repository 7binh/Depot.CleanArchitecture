namespace Depot.CleanArchitecture.Application.Features.Gate.GateIn;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public sealed record GateInCommand(
    Guid TenantId,
    string ContainerNumber,
    int Size,
    ContainerType Type,
    ContainerGrade Grade,
    string LineOperator,
    string TractorNo,
    string TrailerNo,
    string DriverName,
    string DriverPhone,
    string? DamageNotes) : ICommand<Result<GateInResponse>>;

public sealed record GateInResponse(
    Guid VisitId,
    Guid ContainerId,
    string ContainerNumber,
    string Status,
    DateTime GateInDate);
