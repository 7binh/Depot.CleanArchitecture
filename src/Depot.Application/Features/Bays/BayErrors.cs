using Depot.Domain.Common;

namespace Depot.Application.Features.Bays;

internal static class BayErrors
{
    public static Error BlockNotFound(Guid blockId)
    {
        return Error.NotFound("Block.NotFound", $"Block '{blockId}' was not found.");
    }

    public static Error BayNotFound(Guid bayId)
    {
        return Error.NotFound("Bay.NotFound", $"Bay '{bayId}' was not found.");
    }

    public static Error BayNoExists(int bayNo)
    {
        return Error.Conflict("Bay.BayNoExists", $"Bay number {bayNo} already exists in this block.");
    }

    public static Error LayoutChanged()
    {
        return Error.Conflict("Layout.Changed", "The block was changed by another request; nothing was created, try again.");
    }
}
