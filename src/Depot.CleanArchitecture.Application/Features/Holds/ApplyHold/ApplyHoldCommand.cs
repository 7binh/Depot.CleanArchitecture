namespace Depot.CleanArchitecture.Application.Features.Holds.ApplyHold;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record ApplyHoldCommand(
    Guid VisitId,
    string HoldCode,
    string Reason,
    string HeldBy) : ICommand<Result<ApplyHoldResponse>>;

public sealed record ApplyHoldResponse(
    Guid HoldId,
    Guid VisitId,
    string HoldCode,
    DateTime HeldAt);
