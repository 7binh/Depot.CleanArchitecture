using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Reports.YardStock;

public sealed class GetYardStockHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<YardStockQuery, Result<IReadOnlyList<YardStockRow>>>
{
    public async Task<Result<IReadOnlyList<YardStockRow>>> HandleAsync(
        YardStockQuery query,
        CancellationToken cancellationToken = default)
    {
        var zone = timeProvider.LocalTimeZone;
        var today = DepotCalendar.DateOf(timeProvider.GetUtcNow(), zone);
        var reportDate = query.ReportDate ?? today;
        if (reportDate > today)
        {
            return Result.Failure<IReadOnlyList<YardStockRow>>(ReportErrors.ReportDateInFuture());
        }

        // In the yard at the end of the report day: came in before it ended and had not left by then (BR-RP-03).
        var endOfDay = DepotCalendar.StartOf(reportDate.AddDays(1), zone);
        var stays = await dbContext.ContainerStays
            .AsNoTracking()
            .Where(x => x.GateInAt < endOfDay && (x.GateOutAt == null || x.GateOutAt >= endOfDay))
            .Join(dbContext.Containers, x => x.ContainerId, x => x.Id, (stay, container) => new
            {
                container.LineOperatorId,
                stay.GateInAt
            })
            .ToListAsync(cancellationToken);

        var operators = await dbContext.LineOperators
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var rows = stays
            .Select(x => (x.LineOperatorId, Days: StayDuration.DaysBetween(DepotCalendar.DateOf(x.GateInAt, zone), reportDate)))
            .GroupBy(x => x.LineOperatorId)
            .Select(group => new YardStockRow(
                operators[group.Key].Code,
                operators[group.Key].Name,
                group.Count(x => !StayDuration.IsLongStay(x.Days)),
                group.Count(x => StayDuration.IsLongStay(x.Days)),
                group.Count()))
            .OrderBy(x => x.OperatorCode, StringComparer.Ordinal)
            .ToList();

        return Result.Success<IReadOnlyList<YardStockRow>>(rows);
    }
}
