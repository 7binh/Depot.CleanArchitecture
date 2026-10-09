namespace Depot.CleanArchitecture.Application.Features.Gate.GateOut;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record GateOutCommand(
    Guid TenantId,
    string ContainerNumber,
    string OrderNumber,
    string TractorNo,
    string TrailerNo,
    string DriverName,
    string DriverPhone) : ICommand<Result<GateOutResponse>>;

public sealed record GateOutResponse(
    Guid VisitId,
    string ContainerNumber,
    string OrderNumber,
    DateTime GateOutDate);
