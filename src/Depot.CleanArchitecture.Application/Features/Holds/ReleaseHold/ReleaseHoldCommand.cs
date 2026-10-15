namespace Depot.CleanArchitecture.Application.Features.Holds.ReleaseHold;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record ReleaseHoldCommand(
    Guid VisitId,
    Guid HoldId,
    string ReleaseReason,
    string ReleasedBy) : ICommand<Result>;
