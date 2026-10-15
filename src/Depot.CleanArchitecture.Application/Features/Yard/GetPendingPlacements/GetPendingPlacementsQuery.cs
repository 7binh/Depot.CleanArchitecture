namespace Depot.CleanArchitecture.Application.Features.Yard.GetPendingPlacements;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record GetPendingPlacementsQuery() 
    : IQuery<Result<IReadOnlyList<PendingPlacementDto>>>;
