namespace Depot.CleanArchitecture.Application.Features.Holds.GetActiveHolds;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record GetActiveHoldsQuery(Guid? VisitId = null)
    : IQuery<Result<IReadOnlyList<StayHoldDto>>>;

public sealed record StayHoldDto(
    Guid Id,
    Guid VisitId,
    string HoldCode,
    string Reason,
    DateTime HeldAt,
    string HeldBy,
    DateTime? ReleasedAt,
    string? ReleasedBy,
    string? ReleaseReason,
    bool IsActive);
