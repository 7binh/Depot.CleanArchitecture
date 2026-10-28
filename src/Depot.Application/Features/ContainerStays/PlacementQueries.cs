using Depot.Application.Abstractions.Data;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays;

internal static class PlacementQueries
{
    // Loads the block, and for a row also its bay and the tiers already taken. A row that does not match the block
    // is left for the Domain rules to reject, so this method only reports what does not exist.
    // The stay named by excludedStayId (one being moved) is not counted as taking a place.
    public static async Task<Result<GateInPlacement>> LoadPlacementAsync(
        this IAppDbContext dbContext,
        Guid blockId,
        Guid? rowId,
        int? tier,
        Guid? excludedStayId,
        CancellationToken cancellationToken)
    {
        var block = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == blockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<GateInPlacement>(ContainerStayErrors.BlockNotFound(blockId));
        }

        if (rowId is null)
        {
            return Result.Success(new GateInPlacement(block, null, null, tier, []));
        }

        var row = await dbContext.YardRows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == rowId, cancellationToken);
        if (row is null)
        {
            return Result.Failure<GateInPlacement>(ContainerStayErrors.RowNotFound(rowId.Value));
        }

        var bay = await dbContext.Bays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == row.BayId, cancellationToken);
        var occupiedTiers = await OccupiedTiersAsync(dbContext, row.Id, excludedStayId, cancellationToken);
        var neighbours = bay is null
            ? NeighbourTiers.None
            : await dbContext.LoadNeighbourTiersAsync(bay, row, cancellationToken, excludedStayId);

        return Result.Success(new GateInPlacement(block, bay, row, tier, occupiedTiers, neighbours));
    }

    // Only open stays count: a container that has left may come back, and its old position is free again.
    private static async Task<IReadOnlyCollection<int>> OccupiedTiersAsync(
        IAppDbContext dbContext,
        Guid rowId,
        Guid? excludedStayId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ContainerStays
            .Where(x => x.RowId == rowId && x.Tier != null && x.GateOutAt == null && x.Id != excludedStayId)
            .Select(x => x.Tier!.Value)
            .ToListAsync(cancellationToken);
    }
}
