namespace Depot.CleanArchitecture.Application.Features.Yard.GetYardMovements;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record GetYardMovementsQuery(string? ContainerNumber = null, Guid? VisitId = null) 
    : IQuery<Result<List<MovementHistoryItemDto>>>;

public sealed record MovementHistoryItemDto(
    int Stt,
    string Time,
    string FromLocation,
    string ToLocation,
    string Activity,
    string ActivityBadgeClass,
    string PerformedBy,
    string Notes);
