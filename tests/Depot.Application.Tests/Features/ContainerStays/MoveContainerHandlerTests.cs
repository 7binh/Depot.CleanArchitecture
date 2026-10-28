using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.ContainerStays.Move;
using Depot.Application.Features.YardRows.Create;
using Depot.Domain.Common;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerStays;

public sealed class MoveContainerHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheStayIsUnknown()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(Guid.NewGuid(), yard.BlockId, yard.RowId, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerStay.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_MovesTheContainerToAnotherRow_AndKeepsTheStay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var target = await AddRowInBayAsync(dbContext, yard.BlockId, 3);
        var before = await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct);

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(before.Id, yard.BlockId, target, 1), _ct);

        Assert.True(result.IsSuccess);
        var after = await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.GateInAt, after.GateInAt);
        Assert.Equal(target, after.RowId);
        Assert.Equal(1, after.Tier);
        Assert.Null(after.GateOutAt);
    }

    [Fact]
    public async Task HandleAsync_FreesTheOldPosition_ForAnotherContainer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var target = await AddRowInBayAsync(dbContext, yard.BlockId, 3);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;
        await Handler(dbContext).HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, target, 1), _ct);
        var secondId = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);

        var gateIn = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(secondId, yard.BlockId, yard.RowId, 1), _ct);

        Assert.True(gateIn.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheTargetPositionIsTaken()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var target = await AddRowInBayAsync(dbContext, yard.BlockId, 3);
        var secondId = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(secondId, yard.BlockId, target, 1), _ct);
        var stayId = await StayIdOfAsync(dbContext, yard.ContainerId);

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, target, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Occupied", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenAContainerIsStackedAbove()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var target = await AddRowInBayAsync(dbContext, yard.BlockId, 3);
        var topId = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(topId, yard.BlockId, yard.RowId, tier: 2), _ct);
        var stayId = await StayIdOfAsync(dbContext, yard.ContainerId);

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, target, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Stay.HasContainerAbove", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_DoesNotLetAContainerRestOnItsOwnOldPlace()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, yard.RowId, 2), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Tier.BelowEmpty", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenThePositionDoesNotChange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, yard.RowId, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Unchanged", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenTheBayDoesNotTakeThisSize()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var evenBayRow = await AddRowInBayAsync(dbContext, yard.BlockId, 2);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, evenBayRow, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.SizeMismatch", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_MovesTheContainerToAVirtualBlock_WithoutRowAndTier()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var virtualBlock = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand("V", "Bãi ảo", true), _ct);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, virtualBlock.Value.Id, null, null), _ct);

        Assert.True(result.IsSuccess);
        var after = await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct);
        Assert.Equal(virtualBlock.Value.Id, after.BlockId);
        Assert.Null(after.RowId);
        Assert.Null(after.Tier);
        Assert.NotEqual(yard.BlockId, after.BlockId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheContainerHasAlreadyLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var target = await AddRowInBayAsync(dbContext, yard.BlockId, 3);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, yard.BlockId, target, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Stay.AlreadyClosed", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenTheBlockIsUnknown()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var stayId = (await dbContext.ContainerStays.AsNoTracking().SingleAsync(_ct)).Id;

        var result = await Handler(dbContext)
            .HandleAsync(new MoveContainerCommand(stayId, Guid.NewGuid(), null, null), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateIn.BlockNotFound", result.Error.Code);
    }

    private static MoveContainerHandler Handler(AppDbContext dbContext)
    {
        return new MoveContainerHandler(dbContext);
    }

    private static async Task<Guid> StayIdOfAsync(AppDbContext dbContext, Guid containerId)
    {
        var stay = await dbContext.ContainerStays.AsNoTracking().SingleAsync(x => x.ContainerId == containerId, _ct);
        return stay.Id;
    }

    /// <summary>Adds a bay and its row 1 (4 tiers) in the block and returns the row id.</summary>
    private static async Task<Guid> AddRowInBayAsync(AppDbContext dbContext, Guid blockId, int bayNo)
    {
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, bayNo, _ct);
        var row = await new CreateYardRowHandler(dbContext)
            .HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 4), _ct);
        return row.Value.Id;
    }
}
