using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.ContainerStays.GateIn;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerStays;

public sealed class GateInHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_SavesStay_AndUsesNowWhenGateInAtIsEmpty()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(yard.ContainerId, result.Value.ContainerId);
        Assert.Equal(_now, result.Value.GateInAt);
        Assert.Equal(StayGrade.A, result.Value.Grade);
        Assert.Equal("51C-12345", result.Value.InVehicleNumber);
        Assert.Equal(yard.RowId, result.Value.RowId);
        Assert.Equal(1, result.Value.Tier);
        Assert.Equal(1, await dbContext.ContainerStays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_UsesTheGivenGateInTime()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var earlier = _now.AddHours(-3);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId, gateInAt: earlier), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(earlier, result.Value.GateInAt);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenContainerDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(Guid.NewGuid(), yard.BlockId, yard.RowId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateIn.ContainerNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, Guid.NewGuid(), yard.RowId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateIn.BlockNotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenRowDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateIn.RowNotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_AcceptsAVirtualBlock_WithoutRowOrTier()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var virtualBlock = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand("V1", "Virtual 1", true), _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, virtualBlock.Value.Id, null, null), _ct);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.RowId);
        Assert.Null(result.Value.Tier);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_WhenANormalBlockHasNoRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, null, null), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.RowAndTierRequired", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_WhenTheRowIsInAnotherBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var otherBlockId = await YardTestData.CreateBlockAsync(dbContext, "B", _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, otherBlockId, yard.RowId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Inconsistent", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_AndSavesNothing_WhenDamagedWithoutANote()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var command = GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId) with
        {
            Condition = StayCondition.Damaged
        };

        var result = await GateInTestData.CreateHandler(dbContext).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("DamageNote.Required", result.Error.Code);
        Assert.Equal(0, await dbContext.ContainerStays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheContainerIsAlreadyInTheYard()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var handler = GateInTestData.CreateHandler(dbContext);
        await handler.HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        var result = await handler.HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId, 2), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.AlreadyInYard", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.ContainerStays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenAnotherContainerHoldsThePosition()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var secondId = await GateInTestData.CreateSecondContainerAsync(dbContext, _ct);
        var handler = GateInTestData.CreateHandler(dbContext);
        await handler.HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        var result = await handler.HandleAsync(GateInTestData.Command(secondId, yard.BlockId, yard.RowId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Occupied", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_StacksTier2OnTier1_ButNotOnAnEmptyTier()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var secondId = await GateInTestData.CreateSecondContainerAsync(dbContext, _ct);
        var handler = GateInTestData.CreateHandler(dbContext);

        var floating = await handler.HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId, 2), _ct);
        await handler.HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);
        var stacked = await handler.HandleAsync(GateInTestData.Command(secondId, yard.BlockId, yard.RowId, 2), _ct);

        Assert.True(floating.IsFailure);
        Assert.Equal("Tier.BelowEmpty", floating.Error.Code);
        Assert.True(stacked.IsSuccess);
        Assert.Equal(2, stacked.Value.Tier);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenAnotherClerkGatesTheSameContainerInFirst()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var inner = TestDbContextFactory.Create(databaseName);
        await using var rival = TestDbContextFactory.Create(databaseName);
        var yard = await GateInTestData.CreateYardAsync(inner, _ct);
        var command = GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId);
        var racing = new RacingDbContext(
            inner,
            ct => GateInTestData.CreateHandler(rival).HandleAsync(command, ct));
        var handler = new GateInHandler(racing, ContainerTestData.Clock);

        var result = await handler.HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.AlreadyInYard", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
