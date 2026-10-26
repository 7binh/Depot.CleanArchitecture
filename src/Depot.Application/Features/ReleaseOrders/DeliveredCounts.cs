using Depot.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders;

internal static class DeliveredCounts
{
    /// <summary>How many containers have left on each order line (BR-RO-06), keyed by line id.</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> LoadAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        var counts = await dbContext.ContainerStays
            .AsNoTracking()
            .Where(x => x.ReleaseOrderItemId != null && itemIds.Contains(x.ReleaseOrderItemId.Value))
            .GroupBy(x => x.ReleaseOrderItemId!.Value)
            .Select(x => new { ItemId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.ItemId, x => x.Count);
    }
}
