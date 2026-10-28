using Depot.Application.Features.Containers.Search;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.Containers;

public sealed class SearchContainersHandlerTests
{
    private const string SecondNumber = "ABCU1234560";

    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyPage_WhenNoContainerExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new SearchContainersHandler(dbContext);

        var result = await handler.HandleAsync(new SearchContainersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOperatorAndTypeCodes_OrderedByNumber()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var result = await handler.HandleAsync(new SearchContainersQuery(), _ct);

        Assert.Equal([SecondNumber, ContainerTestData.ValidNumber], result.Value.Items.Select(x => x.Number));
        Assert.Equal(["ABC", "CSQ"], result.Value.Items.Select(x => x.LineOperatorCode));
        Assert.Equal(["REEF", "DRY"], result.Value.Items.Select(x => x.ContainerTypeCode));
        Assert.Equal(seed.SecondTypeId, result.Value.Items[0].ContainerTypeId);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnePageAndTheFullTotal_WhenPageSizeIsSmall()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var first = await handler.HandleAsync(new SearchContainersQuery(Page: 1, PageSize: 1), _ct);
        var second = await handler.HandleAsync(new SearchContainersQuery(Page: 2, PageSize: 1), _ct);

        Assert.Equal([SecondNumber], first.Value.Items.Select(x => x.Number));
        Assert.Equal([ContainerTestData.ValidNumber], second.Value.Items.Select(x => x.Number));
        Assert.Equal(2, first.Value.TotalCount);
        Assert.Equal(2, second.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ClampsPageAndPageSize_WhenOutOfRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var result = await handler.HandleAsync(new SearchContainersQuery(Page: 0, PageSize: 10_000), _ct);

        Assert.Equal(1, result.Value.Page);
        Assert.Equal(SearchContainersQuery.MaxPageSize, result.Value.PageSize);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task HandleAsync_FiltersByNumberPart_IgnoringCase()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var result = await handler.HandleAsync(new SearchContainersQuery(Number: " abcu12 "), _ct);

        Assert.Equal([SecondNumber], result.Value.Items.Select(x => x.Number));
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersByOperatorAndByType()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var byOperator = await handler.HandleAsync(new SearchContainersQuery(LineOperatorId: seed.FirstOperatorId), _ct);
        var byType = await handler.HandleAsync(new SearchContainersQuery(ContainerTypeId: seed.SecondTypeId), _ct);
        var neither = await handler.HandleAsync(
            new SearchContainersQuery(LineOperatorId: seed.FirstOperatorId, ContainerTypeId: seed.SecondTypeId),
            _ct);

        Assert.Equal([ContainerTestData.ValidNumber], byOperator.Value.Items.Select(x => x.Number));
        Assert.Equal([SecondNumber], byType.Value.Items.Select(x => x.Number));
        Assert.Empty(neither.Value.Items);
        Assert.Equal(0, neither.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersBySizeAndIsoCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedTwoContainersAsync(dbContext);
        var handler = new SearchContainersHandler(dbContext);

        var size20 = await handler.HandleAsync(new SearchContainersQuery(SizeFeet: 20), _ct);
        var size40 = await handler.HandleAsync(new SearchContainersQuery(SizeFeet: 40), _ct);
        var iso = await handler.HandleAsync(new SearchContainersQuery(IsoCode: "22g"), _ct);

        Assert.Equal(2, size20.Value.TotalCount);
        Assert.Equal(0, size40.Value.TotalCount);
        Assert.Equal(2, iso.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersByWhetherTheContainerIsInTheYard()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var outside = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);
        var handler = new SearchContainersHandler(dbContext);

        var inYard = await handler.HandleAsync(new SearchContainersQuery(InYard: true), _ct);
        var notInYard = await handler.HandleAsync(new SearchContainersQuery(InYard: false), _ct);
        var any = await handler.HandleAsync(new SearchContainersQuery(), _ct);

        Assert.Equal([yard.ContainerId], inYard.Value.Items.Select(x => x.Id));
        Assert.Equal([outside], notInYard.Value.Items.Select(x => x.Id));
        Assert.Equal(2, any.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_LeavesOutAContainerThatHasGateOut()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        var result = await new SearchContainersHandler(dbContext)
            .HandleAsync(new SearchContainersQuery(InYard: true), _ct);

        Assert.Empty(result.Value.Items);
    }

    private static async Task<Seed> SeedTwoContainersAsync(AppDbContext dbContext)
    {
        var firstOperatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        await ContainerTestData.CreateOperatorAsync(dbContext, "ABC", _ct);
        var firstTypeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var secondTypeId = await ContainerTestData.CreateTypeAsync(dbContext, "REEF", _ct);
        var create = ContainerTestData.CreateHandler(dbContext);
        // ABCU1234560: ISO 6346 check digit of ABCU123456 is 0 (sum 5478, remainder 0).
        await create.HandleAsync(ContainerTestData.Command(firstTypeId), _ct);
        await create.HandleAsync(ContainerTestData.Command(secondTypeId, SecondNumber), _ct);
        return new Seed(firstOperatorId, secondTypeId);
    }

    private sealed record Seed(Guid FirstOperatorId, Guid SecondTypeId);
}
