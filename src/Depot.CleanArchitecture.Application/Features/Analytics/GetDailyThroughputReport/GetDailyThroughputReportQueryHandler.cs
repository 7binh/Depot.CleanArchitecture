namespace Depot.CleanArchitecture.Application.Features.Analytics.GetDailyThroughputReport;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class GetDailyThroughputReportQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetDailyThroughputReportQuery, Result<List<DailyThroughputDto>>>
{
    public async Task<Result<List<DailyThroughputDto>>> HandleAsync(
        GetDailyThroughputReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var fromDateTime = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateTime = query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var visits = await dbContext.ContainerVisits
            .AsNoTracking()
            .Where(v => (v.GateInDate >= fromDateTime && v.GateInDate <= toDateTime) ||
                        (v.GateOutDate != null && v.GateOutDate >= fromDateTime && v.GateOutDate <= toDateTime))
            .ToListAsync(cancellationToken);

        var inGroups = visits
            .Where(v => v.GateInDate >= fromDateTime && v.GateInDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateInDate), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = g.Count(), OutCount = 0 });

        var outGroups = visits
            .Where(v => v.GateOutDate != null && v.GateOutDate >= fromDateTime && v.GateOutDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateOutDate!.Value), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = 0, OutCount = g.Count() });

        var combined = inGroups.Concat(outGroups)
            .GroupBy(x => new { x.Date, x.LineOperator })
            .Select(g => new DailyThroughputDto(
                g.Key.Date,
                g.Key.LineOperator,
                g.Sum(x => x.InCount),
                g.Sum(x => x.OutCount),
                g.Sum(x => x.InCount + x.OutCount),
                g.Sum(x => x.InCount + x.OutCount) // Tạm tính 1 cont = 1 TEU
            ))
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.TotalMovements)
            .ToList();

        return Result.Success(combined);
    }
}
