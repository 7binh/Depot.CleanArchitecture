using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Reports.Throughput;

public sealed class GetThroughputHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<ThroughputQuery, Result<IReadOnlyList<ThroughputRow>>>
{
    public async Task<Result<IReadOnlyList<ThroughputRow>>> HandleAsync(
        ThroughputQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.From > query.To)
        {
            return Result.Failure<IReadOnlyList<ThroughputRow>>(ReportErrors.DateRangeInvalid());
        }

        var zone = timeProvider.LocalTimeZone;
        var start = DepotCalendar.StartOf(query.From, zone);
        var end = DepotCalendar.StartOf(query.To.AddDays(1), zone);

        var movements = await dbContext.ContainerStays
            .AsNoTracking()
            .Where(x => (x.GateInAt >= start && x.GateInAt < end) || (x.GateOutAt >= start && x.GateOutAt < end))
            .Join(dbContext.Containers, x => x.ContainerId, x => x.Id, (stay, container) => new Movement(
                container.LineOperatorId,
                stay.GateInAt,
                stay.GateOutAt))
            .ToListAsync(cancellationToken);

        var operators = await dbContext.LineOperators
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var days = movements
            .SelectMany(x => EventsOf(x, start, end))
            .GroupBy(x => (Date: DepotCalendar.DateOf(x.At, zone), x.OperatorId))
            .Select(group => new ThroughputRow(
                group.Key.Date,
                operators[group.Key.OperatorId].Code,
                operators[group.Key.OperatorId].Name,
                group.Count(x => x.IsGateIn),
                group.Count(x => !x.IsGateIn)))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.OperatorCode, StringComparer.Ordinal)
            .ToList();

        return Result.Success<IReadOnlyList<ThroughputRow>>(days);
    }

    // A stay can contribute two events: its gate in and its gate out, each only when it falls inside the period.
    private static IEnumerable<StayEvent> EventsOf(Movement movement, DateTimeOffset start, DateTimeOffset end)
    {
        if (movement.GateInAt >= start && movement.GateInAt < end)
        {
            yield return new StayEvent(movement.OperatorId, movement.GateInAt, true);
        }

        if (movement.GateOutAt >= start && movement.GateOutAt < end)
        {
            yield return new StayEvent(movement.OperatorId, movement.GateOutAt.Value, false);
        }
    }

    private sealed record Movement(Guid OperatorId, DateTimeOffset GateInAt, DateTimeOffset? GateOutAt);

    private sealed record StayEvent(Guid OperatorId, DateTimeOffset At, bool IsGateIn);
}
