using Depot.Application.Features.ReleaseOrders.Search;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.ReleaseOrders;

public sealed class SearchReleaseOrdersHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyPage_WhenNoOrderExists()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Handler(dbContext).HandleAsync(new SearchReleaseOrdersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOneOrderPerPage_WithTheFullTotal()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedOrdersAsync(dbContext);

        var first = await Handler(dbContext).HandleAsync(new SearchReleaseOrdersQuery(Page: 1, PageSize: 1), _ct);
        var third = await Handler(dbContext).HandleAsync(new SearchReleaseOrdersQuery(Page: 3, PageSize: 1), _ct);

        Assert.Single(first.Value.Items);
        Assert.Single(third.Value.Items);
        Assert.NotEqual(first.Value.Items[0].OrderNo, third.Value.Items[0].OrderNo);
        Assert.Equal(3, first.Value.TotalCount);
        Assert.Equal(3, first.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_OpenOnly_LeavesOutExpiredAndFullOrders()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedOrdersAsync(dbContext);

        var result = await Handler(dbContext).HandleAsync(new SearchReleaseOrdersQuery(OpenOnly: true), _ct);

        Assert.Equal(["RO-OPEN"], result.Value.Items.Select(x => x.OrderNo));
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersByLineOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedOrdersAsync(dbContext);
        var otherOperator = await ContainerTestData.CreateOperatorAsync(dbContext, "MSK", _ct);

        var result = await Handler(dbContext)
            .HandleAsync(new SearchReleaseOrdersQuery(LineOperatorId: otherOperator), _ct);

        Assert.Empty(result.Value.Items);
    }

    private static SearchReleaseOrdersHandler Handler(AppDbContext dbContext)
    {
        return new SearchReleaseOrdersHandler(dbContext, ContainerTestData.Clock);
    }

    /// <summary>RO-OPEN has room, RO-FULL has delivered its only container, RO-OLD expired yesterday.</summary>
    private static async Task SeedOrdersAsync(AppDbContext dbContext)
    {
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct, orderNo: "RO-OPEN");
        var full = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct, orderNo: "RO-FULL");
        await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct, expiresInDays: -1, orderNo: "RO-OLD");
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, full), _ct);
    }
}
