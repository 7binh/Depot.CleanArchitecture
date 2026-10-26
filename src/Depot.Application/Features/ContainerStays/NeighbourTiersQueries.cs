using Depot.Application.Abstractions.Data;
using Depot.Domain.Gate;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays;

internal static class NeighbourTiersQueries
{
    // A 40ft container in an even bay also covers the odd bays beside it, so the bays within two numbers of this one,
    // in the rows with the same row number, share its floor space. Only containers still in the yard count.
    public static async Task<NeighbourTiers> LoadNeighbourTiersAsync(
        this IAppDbContext dbContext,
        Bay bay,
        YardRow row,
        CancellationToken cancellationToken)
    {
        var bayNos = new[] { bay.BayNo - 2, bay.BayNo - 1, bay.BayNo + 1, bay.BayNo + 2 };
        var taken = await (
            from stay in dbContext.ContainerStays
            join nearRow in dbContext.YardRows on stay.RowId equals nearRow.Id
            join nearBay in dbContext.Bays on nearRow.BayId equals nearBay.Id
            where stay.GateOutAt == null
                && stay.Tier != null
                && nearBay.BlockId == bay.BlockId
                && nearRow.RowNo == row.RowNo
                && bayNos.Contains(nearBay.BayNo)
            select new { nearBay.BayNo, Tier = stay.Tier!.Value })
            .ToListAsync(cancellationToken);

        var declared = await dbContext.Bays
            .Where(x => x.BlockId == bay.BlockId && (x.BayNo == bay.BayNo - 1 || x.BayNo == bay.BayNo + 1))
            .Select(x => x.BayNo)
            .ToListAsync(cancellationToken);

        var tiers = taken.Select(x => (x.BayNo, x.Tier)).ToList();
        return new NeighbourTiers(
            TiersAt(tiers, bay.BayNo - 1),
            TiersAt(tiers, bay.BayNo + 1),
            TiersAt(tiers, bay.BayNo - 2),
            TiersAt(tiers, bay.BayNo + 2),
            declared.Contains(bay.BayNo - 1),
            declared.Contains(bay.BayNo + 1));
    }

    private static List<int> TiersAt(List<(int BayNo, int Tier)> taken, int bayNo)
    {
        return taken.Where(x => x.BayNo == bayNo).Select(x => x.Tier).ToList();
    }
}
