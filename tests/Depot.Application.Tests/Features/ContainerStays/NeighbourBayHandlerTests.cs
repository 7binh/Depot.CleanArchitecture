using Depot.Application.Features.YardRows.Create;
using Depot.Domain.Common;
using Depot.Domain.Containers.Rules;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerStays;

// A 40ft container in an even bay N also covers the odd bays N-1 and N+1 (BR-YD-04).
public sealed class NeighbourBayHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private sealed record Yard(Guid BlockId, Guid TypeId, Guid[] RowByBay);

    // Block A with bays 1, 2 and 3, each holding row 1 with four tiers. RowByBay[0] is bay 1.
    private static async Task<Yard> CreateYardAsync(AppDbContext dbContext)
    {
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);

        var rows = new List<Guid>();
        for (var bayNo = 1; bayNo <= 3; bayNo++)
        {
            var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, bayNo, _ct);
            var row = await new CreateYardRowHandler(dbContext)
                .HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 4), _ct);
            rows.Add(row.Value.Id);
        }

        return new Yard(blockId, typeId, [.. rows]);
    }

    private static async Task<Guid> CreateContainerAsync(AppDbContext dbContext, Yard yard, int sizeFeet, int serial)
    {
        var prefix = $"CSQU30540{serial}";
        var number = prefix + ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix);
        var command = ContainerTestData.Command(yard.TypeId, number) with { SizeFeet = sizeFeet };
        var container = await ContainerTestData.CreateHandler(dbContext).HandleAsync(command, _ct);
        return container.Value.Id;
    }

    private static async Task<Result> GateInAsync(AppDbContext dbContext, Yard yard, Guid containerId, int bayNo, int tier)
    {
        var command = GateInTestData.Command(containerId, yard.BlockId, yard.RowByBay[bayNo - 1], tier);
        var result = await GateInTestData.CreateHandler(dbContext).HandleAsync(command, _ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    private static void AssertError(Result result, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task GateIn_BlocksAFortyFootContainer_WhereATwentyFootOneAlreadyStands()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var twenty = await CreateContainerAsync(dbContext, yard, 20, 1);
        var forty = await CreateContainerAsync(dbContext, yard, 40, 2);
        Assert.True((await GateInAsync(dbContext, yard, twenty, 1, 1)).IsSuccess);

        AssertError(await GateInAsync(dbContext, yard, forty, 2, 1), "Position.OverlapsNeighbour");
    }

    [Fact]
    public async Task GateIn_BlocksATwentyFootContainer_WhereAFortyFootOneCoversTheBay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var forty = await CreateContainerAsync(dbContext, yard, 40, 1);
        var twenty = await CreateContainerAsync(dbContext, yard, 20, 2);
        Assert.True((await GateInAsync(dbContext, yard, forty, 2, 1)).IsSuccess);

        AssertError(await GateInAsync(dbContext, yard, twenty, 1, 1), "Position.OverlapsNeighbour");
        AssertError(await GateInAsync(dbContext, yard, twenty, 3, 1), "Position.OverlapsNeighbour");
    }

    [Fact]
    public async Task GateIn_AllowsAFortyFootContainerOnTwoTwentyFootOnes_ButNotOnOne()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var left = await CreateContainerAsync(dbContext, yard, 20, 1);
        var right = await CreateContainerAsync(dbContext, yard, 20, 2);
        var forty = await CreateContainerAsync(dbContext, yard, 40, 3);
        Assert.True((await GateInAsync(dbContext, yard, left, 1, 1)).IsSuccess);

        AssertError(await GateInAsync(dbContext, yard, forty, 2, 2), "Tier.BelowEmpty");

        Assert.True((await GateInAsync(dbContext, yard, right, 3, 1)).IsSuccess);
        Assert.True((await GateInAsync(dbContext, yard, forty, 2, 2)).IsSuccess);
    }

    [Fact]
    public async Task GateOut_BlocksATwentyFootContainer_WhileAFortyFootOneRestsOnIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var left = await CreateContainerAsync(dbContext, yard, 20, 1);
        var right = await CreateContainerAsync(dbContext, yard, 20, 2);
        var forty = await CreateContainerAsync(dbContext, yard, 40, 3);
        await GateInAsync(dbContext, yard, left, 1, 1);
        await GateInAsync(dbContext, yard, right, 3, 1);
        await GateInAsync(dbContext, yard, forty, 2, 2);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(left, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Stay.HasContainerAbove", result.Error.Code);
        Assert.Equal(3, await dbContext.ContainerStays.CountAsync(x => x.GateOutAt == null, _ct));
    }
}
