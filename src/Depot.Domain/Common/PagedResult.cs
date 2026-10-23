namespace Depot.Domain.Common;

/// <summary>One page of a larger list, plus the numbers a screen needs to draw its pager.</summary>
public sealed class PagedResult<T>(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
