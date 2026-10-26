using Depot.Application.Abstractions.Data;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Bays;

// "Stands" means a container still in the yard (BR-YD-09); one that has left needs nothing from the structure.
internal static class YardOccupancyQueries
{
    public static Task<bool> AnyStandsInRowAsync(
        this IAppDbContext dbContext,
        Guid rowId,
        CancellationToken cancellationToken)
    {
        return dbContext.ContainerStays.AnyAsync(x => x.RowId == rowId && x.GateOutAt == null, cancellationToken);
    }

    public static Task<bool> AnyStandsInBayAsync(
        this IAppDbContext dbContext,
        Guid bayId,
        CancellationToken cancellationToken)
    {
        return dbContext.ContainerStays.AnyAsync(
            x => x.GateOutAt == null
                && dbContext.YardRows.Any(row => row.Id == x.RowId && row.BayId == bayId),
            cancellationToken);
    }

    // A 40ft container in an even bay N lies over the odd bays N-1 and N+1 (BR-YD-04), so it also stands on their
    // floor space even though its own row is in bay N. With a row number, only that row of the odd bay is meant.
    public static async Task<bool> AnyStandsOverAsync(
        this IAppDbContext dbContext,
        Bay bay,
        int? rowNo,
        CancellationToken cancellationToken)
    {
        if (bay.BayNo % 2 == 0)
        {
            return false;
        }

        return await (
            from stay in dbContext.ContainerStays
            join row in dbContext.YardRows on stay.RowId equals row.Id
            join nearBay in dbContext.Bays on row.BayId equals nearBay.Id
            where stay.GateOutAt == null
                && nearBay.BlockId == bay.BlockId
                && (nearBay.BayNo == bay.BayNo - 1 || nearBay.BayNo == bay.BayNo + 1)
                && (rowNo == null || row.RowNo == rowNo)
            select stay.Id)
            .AnyAsync(cancellationToken);
    }

    // Closed stays keep their history but lose the position, so the row can go while the history stays (BR-YD-09).
    // The changes are saved together with the delete by the caller.
    public static async Task<Result> ForgetRowsInClosedStaysAsync(
        this IAppDbContext dbContext,
        IReadOnlyCollection<Guid> rowIds,
        CancellationToken cancellationToken)
    {
        var closed = await dbContext.ContainerStays
            .Where(x => x.GateOutAt != null && x.RowId != null && rowIds.Contains(x.RowId.Value))
            .ToListAsync(cancellationToken);

        foreach (var stay in closed)
        {
            var forgotten = stay.ForgetRow();
            if (forgotten.IsFailure)
            {
                return forgotten;
            }
        }

        return Result.Success();
    }
}
