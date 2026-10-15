namespace Depot.CleanArchitecture.Application.Features.Inspections.SubmitInspectionSurvey;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public sealed record SubmitInspectionSurveyCommand(
    Guid VisitId,
    ContainerGrade Grade,
    string Condition,
    List<string> DamageCodes,
    string? Notes,
    string? PhotoRefs,
    string Inspector) : ICommand<Result<SubmitInspectionSurveyResponse>>;

public sealed record SubmitInspectionSurveyResponse(
    Guid SurveyId,
    Guid VisitId,
    ContainerGrade Grade,
    bool IsDamageHoldTriggered,
    DateTime InspectedAt);
