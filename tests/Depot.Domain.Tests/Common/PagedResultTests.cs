using Depot.Domain.Common;

namespace Depot.Domain.Tests.Common;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp_WhenLastPageIsPartial()
    {
        var items = new List<int> { 1, 2, 3 };

        var paged = new PagedResult<int>(items, totalCount: 25, page: 1, pageSize: 10);

        Assert.Equal(3, paged.TotalPages);
    }

    [Fact]
    public void TotalPages_IsZero_WhenPageSizeIsZero()
    {
        var items = new List<int> { 1, 2, 3 };

        var paged = new PagedResult<int>(items, totalCount: 25, page: 1, pageSize: 0);

        Assert.Equal(0, paged.TotalPages);
    }

    [Fact]
    public void FirstPage_HasNextButNoPrevious()
    {
        var items = new List<int> { 1, 2, 3 };

        var paged = new PagedResult<int>(items, totalCount: 25, page: 1, pageSize: 10);

        Assert.False(paged.HasPreviousPage);
        Assert.True(paged.HasNextPage);
    }
}
