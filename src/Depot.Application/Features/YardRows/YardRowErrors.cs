using Depot.Domain.Common;

namespace Depot.Application.Features.YardRows;

internal static class YardRowErrors
{
    public static Error RowNotFound(Guid rowId)
    {
        return Error.NotFound("YardRow.NotFound", $"Row '{rowId}' was not found.");
    }

    public static Error RowNoExists(int rowNo)
    {
        return Error.Conflict("YardRow.RowNoExists", $"Row number {rowNo} already exists in this bay.");
    }
}
