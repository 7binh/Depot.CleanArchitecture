using Depot.Domain.Common;

namespace Depot.Application.Features.ContainerStays;

internal static class ContainerStayErrors
{
    // The container, block and row ids all come from the request body, so a missing one is a 400, not a 404.
    public static Error ContainerNotFound(Guid containerId)
    {
        return Error.Validation("GateIn.ContainerNotFound", $"Container '{containerId}' was not found.");
    }

    public static Error BlockNotFound(Guid blockId)
    {
        return Error.Validation("GateIn.BlockNotFound", $"Block '{blockId}' was not found.");
    }

    public static Error RowNotFound(Guid rowId)
    {
        return Error.Validation("GateIn.RowNotFound", $"Row '{rowId}' was not found.");
    }

    public static Error GateOutContainerNotFound(Guid containerId)
    {
        return Error.Validation("GateOut.ContainerNotFound", $"Container '{containerId}' was not found.");
    }

    public static Error GateOutReleaseOrderNotFound(Guid releaseOrderId)
    {
        return Error.Validation("GateOut.ReleaseOrderNotFound", $"Release order '{releaseOrderId}' was not found.");
    }

    public static Error NotInYard(Guid containerId)
    {
        return Error.Conflict("GateOut.ContainerNotInYard", $"Container '{containerId}' is not in the yard.");
    }

    public static Error StayNotFound(Guid stayId)
    {
        return Error.NotFound("ContainerStay.NotFound", $"Container stay '{stayId}' was not found.");
    }
}
