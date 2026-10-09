namespace Depot.CleanArchitecture.Application.Features.Yard.PlaceContainer;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record PlaceContainerCommand(
    Guid TenantId,
    Guid VisitId,
    Guid BlockId,
    int Bay,
    int Row,
    int Tier) : ICommand<Result<PlaceContainerResponse>>;

public sealed record PlaceContainerResponse(
    Guid VisitId,
    Guid SlotId,
    string CoordinateDescription);
