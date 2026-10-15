namespace Depot.CleanArchitecture.Application.Features.Yard.RelocateContainer;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record RelocateContainerCommand(
    Guid VisitId,
    Guid TargetBlockId,
    int TargetBay,
    int TargetRow,
    int TargetTier,
    string ActorId,
    string? Reason = null) : ICommand<Result<RelocateContainerResponse>>;

public sealed record RelocateContainerResponse(
    Guid VisitId,
    string ContainerNumber,
    string? FromBlockCode,
    string? FromCoordinates,
    string ToBlockCode,
    string ToCoordinates,
    bool IsVirtualBlock,
    DateTime RelocatedAt);
