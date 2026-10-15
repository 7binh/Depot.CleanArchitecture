namespace Depot.CleanArchitecture.Application.Features.Inspections.GetVisitSurveys;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public sealed record GetVisitSurveysQuery(Guid VisitId)
    : IQuery<Result<IReadOnlyList<InspectionSurveyDto>>>;

public sealed record InspectionSurveyDto(
    Guid Id,
    Guid VisitId,
    ContainerGrade Grade,
    string Condition,
    List<string> DamageCodes,
    string? Notes,
    string? PhotoRefs,
    DateTime InspectedAt,
    string InspectedBy);
