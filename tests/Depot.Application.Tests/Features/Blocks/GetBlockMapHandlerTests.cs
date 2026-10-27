using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GenerateLayout;
using Depot.Application.Features.Blocks.GetMap;
using Depot.Application.Features.ContainerStays.GateIn;
using Depot.Application.Features.Containers.Create;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Domain.Containers.Rules;
using Depot.Domain.Gate;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class GetBlockMapHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // The test clock reads 2026-10-26 12:00 UTC.
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static int _serial = 100000;

    private static async Task<Guid> CreateBlockAsync(
        AppDbContext dbContext,
        string code,
        int fromBay = 1,
        int toBay = 5,
        bool isVirtual = false)
    {
        var block = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand(code, $"Block {code}", isVirtual), _ct);
        if (!isVirtual)
        {
            await new GenerateLayoutHandler(dbContext)
                .HandleAsync(new GenerateLayoutCommand(block.Value.Id, fromBay, toBay, 2, 4), _ct);
        }

        return block.Value.Id;
    }

    private static async Task EnsureCatalogAsync(AppDbContext dbContext)
    {
        if (!await dbContext.LineOperators.AnyAsync(_ct))
        {
            await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
            await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        }
    }

    private static async Task<Guid> CreateContainerAsync(AppDbContext dbContext, int sizeFeet)
    {
        await EnsureCatalogAsync(dbContext);
        var prefix = $"CSQU{_serial++}";
        var number = prefix + ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix);
        var typeId = await dbContext.ContainerTypes.Where(x => x.Code == "DRY").Select(x => x.Id).SingleAsync(_ct);
        var iso = sizeFeet == 20 ? "22G1" : "42G1";
        var command = new CreateContainerCommand(number, typeId, iso, sizeFeet, 30480, 2200, new DateOnly(2020, 5, 1));
        var created = await ContainerTestData.CreateHandler(dbContext).HandleAsync(command, _ct);
        Assert.True(created.IsSuccess);
        return created.Value.Id;
    }

    private static async Task<Guid> PlaceAsync(
        AppDbContext dbContext,
        Guid blockId,
        int bayNo,
        int rowNo,
        int tier,
        int sizeFeet = 20,
        DateTimeOffset? gateInAt = null,
        StayCondition condition = StayCondition.Normal)
    {
        var containerId = await CreateContainerAsync(dbContext, sizeFeet);
        var rowId = await dbContext.YardRows
            .Where(x => x.RowNo == rowNo)
            .Join(dbContext.Bays.Where(x => x.BlockId == blockId && x.BayNo == bayNo), row => row.BayId, bay => bay.Id, (row, _) => row.Id)
            .SingleAsync(_ct);
        var command = new GateInCommand(
            containerId,
            gateInAt ?? _now.AddDays(-1),
            StayGrade.A,
            condition,
            condition == StayCondition.Damaged ? "Dented side wall" : null,
            VehicleType.Truck,
            "51C-12345",
            blockId,
            rowId,
            tier);
        var stay = await GateInTestData.CreateHandler(dbContext).HandleAsync(command, _ct);
        Assert.True(stay.IsSuccess);
        return containerId;
    }

    private static GetBlockMapHandler Handler(AppDbContext dbContext)
    {
        return new GetBlockMapHandler(dbContext, ContainerTestData.Clock);
    }

    [Fact]
    public async Task HandleAsync_ListsEveryBayAndRow_WithEmptyStacks_WhenNothingStands()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        Assert.True(result.IsSuccess);
        var map = result.Value;
        Assert.Equal(("A", false), (map.Code, map.IsVirtual));
        Assert.Equal([1, 2, 3, 4, 5], map.Bays.Select(x => x.BayNo));
        Assert.Equal([20, 40, 20, 40, 20], map.Bays.Select(x => x.SizeFeet));
        Assert.All(map.Bays, bay => Assert.Equal([1, 2], bay.Rows.Select(x => x.RowNo)));
        Assert.All(map.Bays.SelectMany(x => x.Rows), row => Assert.Equal(4, row.MaxTier));
        Assert.All(map.Bays.SelectMany(x => x.Rows), row => Assert.Empty(row.Tiers));
        Assert.Empty(map.Unplaced);
    }

    [Fact]
    public async Task HandleAsync_PutsATwentyFootContainerOnItsTier_WithItsOwnBayAsTheSpan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");
        var containerId = await PlaceAsync(dbContext, blockId, 3, 2, 1);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        var row = result.Value.Bays.Single(x => x.BayNo == 3).Rows.Single(x => x.RowNo == 2);
        var tier = Assert.Single(row.Tiers);
        Assert.Equal((1, 3, 3), (tier.Tier, tier.FromBay, tier.ToBay));
        var number = (await dbContext.Containers.SingleAsync(x => x.Id == containerId, _ct)).Number;
        Assert.Equal(number, tier.Container.Number);
        Assert.Equal(("CSQ", 20, false), (tier.Container.OperatorCode, tier.Container.SizeFeet, tier.Container.IsDamaged));
        Assert.Empty(result.Value.Bays.Single(x => x.BayNo == 3).Rows.Single(x => x.RowNo == 1).Tiers);
    }

    [Fact]
    public async Task HandleAsync_ListsAStackFromTheBottomUp()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");
        await PlaceAsync(dbContext, blockId, 1, 1, 1);
        await PlaceAsync(dbContext, blockId, 1, 1, 2);
        await PlaceAsync(dbContext, blockId, 1, 1, 3);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        var row = result.Value.Bays.Single(x => x.BayNo == 1).Rows.Single(x => x.RowNo == 1);
        Assert.Equal([1, 2, 3], row.Tiers.Select(x => x.Tier));
    }

    [Theory]
    [InlineData(2, 1, 3)]
    [InlineData(4, 3, 5)]
    public async Task HandleAsync_GivesAFortyFootContainerTheOddBaysBesideItsEvenBay(int bayNo, int from, int to)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");
        await PlaceAsync(dbContext, blockId, bayNo, 1, 1, 40);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        var tier = result.Value.Bays.Single(x => x.BayNo == bayNo).Rows.Single(x => x.RowNo == 1).Tiers.Single();
        Assert.Equal((from, to, 40), (tier.FromBay, tier.ToBay, tier.Container.SizeFeet));
    }

    [Fact]
    public async Task HandleAsync_LeavesOutAContainerThatHasLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");
        var containerId = await PlaceAsync(dbContext, blockId, 1, 1, 1);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        var gateOut = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(containerId, orderId), _ct);
        Assert.True(gateOut.IsSuccess);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        Assert.All(result.Value.Bays.SelectMany(x => x.Rows), row => Assert.Empty(row.Tiers));
    }

    [Fact]
    public async Task HandleAsync_ShowsOnlyTheContainersOfTheAskedBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockA = await CreateBlockAsync(dbContext, "A");
        var blockB = await CreateBlockAsync(dbContext, "B");
        await PlaceAsync(dbContext, blockA, 1, 1, 1);
        await PlaceAsync(dbContext, blockB, 1, 1, 1);
        await PlaceAsync(dbContext, blockB, 3, 2, 1);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockA), _ct);

        Assert.Equal(1, result.Value.Bays.SelectMany(x => x.Rows).Sum(x => x.Tiers.Count));
    }

    [Fact]
    public async Task HandleAsync_ListsOnlyTheBaysThatExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A", 1, 2);
        await new GenerateLayoutHandler(dbContext).HandleAsync(new GenerateLayoutCommand(blockId, 5, 5, 2, 4), _ct);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        Assert.Equal([1, 2, 5], result.Value.Bays.Select(x => x.BayNo));
    }

    [Fact]
    public async Task HandleAsync_ListsTheContainersOfAVirtualBlockAsUnplaced()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "V", isVirtual: true);
        var containerId = await CreateContainerAsync(dbContext, 20);
        var gateIn = new GateInCommand(containerId, _now.AddDays(-2), StayGrade.A, StayCondition.Normal, null, VehicleType.Truck, "51C-1", blockId, null, null);
        Assert.True((await GateInTestData.CreateHandler(dbContext).HandleAsync(gateIn, _ct)).IsSuccess);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        Assert.True(result.Value.IsVirtual);
        Assert.Empty(result.Value.Bays);
        var unplaced = Assert.Single(result.Value.Unplaced);
        Assert.Equal(2, unplaced.DaysInYard);
    }

    [Fact]
    public async Task HandleAsync_MarksALongStayFromTenDays_AndADamagedContainer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, "A");
        await PlaceAsync(dbContext, blockId, 1, 1, 1, gateInAt: new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero));
        await PlaceAsync(dbContext, blockId, 1, 2, 1, gateInAt: new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero), condition: StayCondition.Damaged);

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(blockId), _ct);

        var rows = result.Value.Bays.Single(x => x.BayNo == 1).Rows;
        var recent = rows.Single(x => x.RowNo == 1).Tiers.Single().Container;
        var long10 = rows.Single(x => x.RowNo == 2).Tiers.Single().Container;
        Assert.Equal((9, false, false), (recent.DaysInYard, recent.IsLongStay, recent.IsDamaged));
        Assert.Equal((10, true, true), (long10.DaysInYard, long10.IsLongStay, long10.IsDamaged));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_ForAnUnknownBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Handler(dbContext).HandleAsync(new GetBlockMapQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
