namespace Depot.Application.Features.ReleaseOrders.Search;

/// <param name="OpenOnly">True keeps only orders not expired today that still have room on some line.</param>
/// <param name="LineOperatorId">Keeps only orders of this line operator.</param>
/// <param name="Status">Keeps only orders that show this status in a list.</param>
/// <param name="OrderNo">Part of the order number; the case typed, upper-case and lower-case are all matched.</param>
/// <param name="Page">Starts at 1; a smaller value is read as 1.</param>
/// <param name="PageSize">Kept between 1 and <see cref="MaxPageSize"/>.</param>
public sealed record SearchReleaseOrdersQuery(
    bool OpenOnly = false,
    Guid? LineOperatorId = null,
    ReleaseOrderListStatus? Status = null,
    string? OrderNo = null,
    int Page = 1,
    int PageSize = SearchReleaseOrdersQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
