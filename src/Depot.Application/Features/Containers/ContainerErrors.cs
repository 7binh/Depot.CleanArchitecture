using Depot.Domain.Common;

namespace Depot.Application.Features.Containers;

internal static class ContainerErrors
{
    // The owner code and the container type id both come from the request body, so a missing one is a 400, not a 404.
    public static Error OwnerCodeMissing()
    {
        return Error.Validation(
            "ContainerNumber.InvalidFormat",
            "ContainerNumber must start with a 3-letter owner code.");
    }

    public static Error OperatorNotFound(string ownerCode)
    {
        return Error.Validation(
            "Container.OperatorNotFound",
            $"Owner code '{ownerCode}' does not belong to any line operator.");
    }

    public static Error ContainerTypeNotFound(Guid containerTypeId)
    {
        return Error.Validation(
            "Container.TypeNotFound",
            $"Container type '{containerTypeId}' was not found.");
    }

    public static Error NumberExists(string number)
    {
        return Error.Conflict("Container.NumberExists", $"Container number '{number}' already exists.");
    }

    public static Error ContainerNotFound(Guid containerId)
    {
        return Error.NotFound("Container.NotFound", $"Container '{containerId}' was not found.");
    }
}
