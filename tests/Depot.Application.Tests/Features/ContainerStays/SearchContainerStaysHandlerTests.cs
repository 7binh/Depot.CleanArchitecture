using Depot.Application.Features.ContainerStays.Search;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.ContainerStays;

public sealed class SearchContainerStaysHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ReturnsEmptyPage_WhenNoStayExists()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewestFirst_OnePagePerRequest_WithTheFullTotal()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await TwoStaysInYardAsync(dbContext);

        var first = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(Page: 1, PageSize: 1), _ct);
        var second = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(Page: 2, PageSize: 1), _ct);

        Assert.Equal([GateInTestData.SecondNumber], first.Value.Items.Select(x => x.ContainerNumber));
        Assert.Equal(["CSQU3054383"], second.Value.Items.Select(x => x.ContainerNumber));
        Assert.Equal(2, first.Value.TotalCount);
        Assert.Equal(2, first.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_ShowsTheContainerNumberAndOperatorOnEachStay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await TwoStaysInYardAsync(dbContext);

        var result = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(), _ct);

        Assert.Equal(["ABC", "CSQ"], result.Value.Items.Select(x => x.LineOperatorCode));
    }

    [Fact]
    public async Task HandleAsync_FiltersByPartOfTheNumber_InAnyCase()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await TwoStaysInYardAsync(dbContext);

        var result = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(Number: " abcu12 "), _ct);

        Assert.Equal([GateInTestData.SecondNumber], result.Value.Items.Select(x => x.ContainerNumber));
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_SeparatesStaysInTheYardFromStaysThatEnded()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);
        var handler = new SearchContainerStaysHandler(dbContext);

        var inYard = await handler.HandleAsync(new SearchContainerStaysQuery(InYard: true), _ct);
        var left = await handler.HandleAsync(new SearchContainerStaysQuery(InYard: false), _ct);

        Assert.Empty(inYard.Value.Items);
        Assert.Equal([yard.ContainerId], left.Value.Items.Select(x => x.ContainerId));
        Assert.NotNull(left.Value.Items[0].GateOutAt);
    }

    [Fact]
    public async Task HandleAsync_ClampsAPageSizeThatIsTooBig()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new SearchContainerStaysHandler(dbContext)
            .HandleAsync(new SearchContainerStaysQuery(Page: 0, PageSize: 5000), _ct);

        Assert.Equal(1, result.Value.Page);
        Assert.Equal(SearchContainerStaysQuery.MaxPageSize, result.Value.PageSize);
    }

    /// <summary>CSQU3054383 came in five hours ago and ABCU1234560 an hour ago, both still in the yard.</summary>
    private static async Task TwoStaysInYardAsync(AppDbContext dbContext)
    {
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var secondId = await GateInTestData.CreateSecondContainerAsync(dbContext, _ct);
        var handler = GateInTestData.CreateHandler(dbContext);
        await handler.HandleAsync(
            GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId, gateInAt: _now.AddHours(-5)),
            _ct);
        await handler.HandleAsync(
            GateInTestData.Command(secondId, yard.BlockId, yard.RowId, 2, _now.AddHours(-1)),
            _ct);
    }
}
