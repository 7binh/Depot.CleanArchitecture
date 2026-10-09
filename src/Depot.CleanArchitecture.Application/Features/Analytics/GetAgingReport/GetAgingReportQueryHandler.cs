namespace Depot.CleanArchitecture.Application.Features.Analytics.GetAgingReport;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class GetAgingReportQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAgingReportQuery, Result<List<LineOperatorAgingDto>>>
{
    public async Task<Result<List<LineOperatorAgingDto>>> HandleAsync(
        GetAgingReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var visits = await dbContext.ContainerVisits
            .AsNoTracking()
            .Where(v => v.Status != ContainerVisitStatus.GatedOut && v.Status != ContainerVisitStatus.Completed)
            .ToListAsync(cancellationToken);

        var report = visits
            .GroupBy(v => v.LineOperator)
            .Select(g =>
            {
                int count0To10 = g.Count(v => (now - v.GateInDate).TotalDays <= 10);
                int countOver10 = g.Count(v => (now - v.GateInDate).TotalDays > 10);
                return new LineOperatorAgingDto(g.Key, count0To10, countOver10, g.Count());
            })
            .OrderByDescending(r => r.TotalInventory)
            .ToList();

        return Result.Success(report);
    }
}
