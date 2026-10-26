using Depot.Domain.Common;

namespace Depot.Application.Features.ReleaseOrders;

internal static class ReleaseOrderErrors
{
    // The operator, customer and container type ids all come from the request body, so a missing one is a 400, not a 404.
    public static Error OperatorNotFound(Guid lineOperatorId)
    {
        return Error.Validation("ReleaseOrder.OperatorNotFound", $"Line operator '{lineOperatorId}' was not found.");
    }

    public static Error CustomerNotFound(Guid customerId)
    {
        return Error.Validation("ReleaseOrder.CustomerNotFound", $"Customer '{customerId}' was not found.");
    }

    public static Error ContainerTypeNotFound(Guid containerTypeId)
    {
        return Error.Validation("ReleaseOrder.TypeNotFound", $"Container type '{containerTypeId}' was not found.");
    }

    public static Error OrderNoExists(string orderNo)
    {
        return Error.Conflict(
            "ReleaseOrder.OrderNoExists",
            $"Release order '{orderNo}' already exists for this line operator.");
    }

    public static Error ReleaseOrderNotFound(Guid releaseOrderId)
    {
        return Error.NotFound("ReleaseOrder.NotFound", $"Release order '{releaseOrderId}' was not found.");
    }
}
