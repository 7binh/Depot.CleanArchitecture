using Depot.Application.Abstractions.Data;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays;

internal static class StackQueries
{
    // BR-YD-06: a container on a higher tier of the same row is still resting on this one. A virtual block has no tiers.
    public static async Task<bool> HasContainerAboveAsync(
        this IAppDbContext dbContext,
        ContainerStay stay,
        CancellationToken cancellationToken)
    {
        if (stay.RowId is null || stay.Tier is null)
        {
            return false;
        }

        var stackedInRow = await dbContext.ContainerStays
            .AnyAsync(x => x.RowId == stay.RowId && x.GateOutAt == null && x.Tier > stay.Tier, cancellationToken);

        return stackedInRow || await IsCarryingFortyFootAsync(dbContext, stay, cancellationToken);
    }

    // A 20ft container in an odd bay can carry a 40ft one whose own row is in the even bay beside it.
    private static async Task<bool> IsCarryingFortyFootAsync(
        IAppDbContext dbContext,
        ContainerStay stay,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.YardRows.AsNoTracking().FirstAsync(x => x.Id == stay.RowId, cancellationToken);
        var bay = await dbContext.Bays.AsNoTracking().FirstAsync(x => x.Id == row.BayId, cancellationToken);
        if (bay.BayNo % 2 == 0)
        {
            return false;
        }

        var neighbours = await dbContext.LoadNeighbourTiersAsync(bay, row, cancellationToken);
        return neighbours.Before.Concat(neighbours.After).Any(tier => tier > stay.Tier);
    }
}
