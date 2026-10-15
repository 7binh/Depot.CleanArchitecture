namespace Depot.CleanArchitecture.Application.Features.Inspections.GetVisitSurveys;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class GetVisitSurveysQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetVisitSurveysQuery, Result<IReadOnlyList<InspectionSurveyDto>>>
{
    public async Task<Result<IReadOnlyList<InspectionSurveyDto>>> HandleAsync(
        GetVisitSurveysQuery query,
        CancellationToken cancellationToken = default)
    {
        var surveys = await dbContext.ConditionSurveys
            .Where(s => s.VisitId == query.VisitId)
            .OrderByDescending(s => s.InspectedAt)
            .Select(s => new InspectionSurveyDto(
                s.Id,
                s.VisitId,
                s.Grade,
                s.Condition,
                s.DamageCodes,
                s.Notes,
                s.PhotoRefs,
                s.InspectedAt,
                s.InspectedBy))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<InspectionSurveyDto>>(surveys);
    }
}
