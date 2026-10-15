namespace Depot.CleanArchitecture.Application.Features.Inspections.SubmitInspectionSurvey;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class SubmitInspectionSurveyCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitInspectionSurveyCommand, Result<SubmitInspectionSurveyResponse>>
{
    public async Task<Result<SubmitInspectionSurveyResponse>> HandleAsync(
        SubmitInspectionSurveyCommand command,
        CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.ContainerVisits
            .Include(v => v.Holds)
            .Include(v => v.Surveys)
            .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

        if (visit is null)
        {
            return Result.Failure<SubmitInspectionSurveyResponse>(
                Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt lưu bãi có mã '{command.VisitId}'!"));
        }

        var surveyResult = ConditionSurvey.Create(
            visit.TenantId,
            visit.Id,
            command.Grade,
            command.Condition,
            command.DamageCodes,
            command.Notes,
            command.PhotoRefs,
            command.Inspector);

        if (surveyResult.IsFailure)
        {
            return Result.Failure<SubmitInspectionSurveyResponse>(surveyResult.Error);
        }

        var survey = surveyResult.Value!;
        var autoHold = visit.AddSurvey(survey);
        dbContext.ConditionSurveys.Add(survey);

        if (autoHold is not null)
        {
            dbContext.StayHolds.Add(autoHold);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var isDamageHoldTriggered = command.Grade is ContainerGrade.GradeD_Damaged or ContainerGrade.GradeE_Scrap;
        var response = new SubmitInspectionSurveyResponse(
            survey.Id,
            visit.Id,
            survey.Grade,
            isDamageHoldTriggered,
            survey.InspectedAt);

        return Result.Success(response);
    }
}
