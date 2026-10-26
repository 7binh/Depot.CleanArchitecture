using Depot.Application.Features.ContainerStays.GateOut;
using Depot.Application.Features.ContainerStays.GetAll;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerStays;

public sealed class GateOutHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ClosesTheStay_AndLinksTheOrderLine()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_now, result.Value.GateOutAt);
        Assert.Equal(VehicleType.Truck, result.Value.OutVehicleType);
        Assert.Equal("51C-99999", result.Value.OutVehicleNumber);
        var itemId = (await dbContext.ReleaseOrders.Include(x => x.Items).SingleAsync(_ct)).Items.Single().Id;
        Assert.Equal(itemId, result.Value.ReleaseOrderItemId);
        var saved = await dbContext.ContainerStays.SingleAsync(_ct);
        Assert.False(saved.IsOpen);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenContainerDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(Guid.NewGuid(), orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOut.ContainerNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenReleaseOrderDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOut.ReleaseOrderNotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheContainerWasNeverGatedIn()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOut.ContainerNotInYard", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheContainerHasAlreadyLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);
        var handler = GateOutTestData.CreateHandler(dbContext);
        await handler.HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        var result = await handler.HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOut.ContainerNotInYard", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_AndKeepsTheStayOpen_WhenTheOrderIsOfAnotherOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var otherOperatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "MSC", _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct, operatorId: otherOperatorId);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.OperatorMismatch", result.Error.Code);
        Assert.True((await dbContext.ContainerStays.SingleAsync(_ct)).IsOpen);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheOrderHasExpired()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct, expiresInDays: -1);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.Expired", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_AcceptsTheExpiryDay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct, expiresInDays: 0);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_WhenGateOutIsBeforeGateIn()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId, _now.AddHours(-1)), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOutAt.BeforeGateIn", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTheLineIsAlreadyDeliveredInFull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var first = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        var handler = GateOutTestData.CreateHandler(dbContext);
        await handler.HandleAsync(GateOutTestData.Command(first.ContainerId, orderId), _ct);
        var secondId = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(secondId, first.BlockId, first.RowId), _ct);

        var result = await handler.HandleAsync(GateOutTestData.Command(secondId, orderId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.LineFull", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenAnotherClerkGatesTheSameContainerOutFirst()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var inner = TestDbContextFactory.Create(databaseName);
        await using var rival = TestDbContextFactory.Create(databaseName);
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(inner, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(inner, 2, _ct);
        var command = GateOutTestData.Command(yard.ContainerId, orderId);
        var racing = new RacingDbContext(
            inner,
            ct => GateOutTestData.CreateHandler(rival).HandleAsync(command, ct),
            () => new DbUpdateConcurrencyException("Simulated stale xmin."));

        var result = await new GateOutHandler(racing, ContainerTestData.Clock).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("GateOut.ContainerNotInYard", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task GateIn_FreesThePositionAndAllowsReturn_AfterTheContainerLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, await dbContext.ContainerStays.CountAsync(_ct));
    }

    [Fact]
    public async Task GetAll_InYardLeavesOutContainersThatHaveLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var first = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(first.ContainerId, orderId), _ct);
        var secondId = await GateOutTestData.CreateSecondCsqContainerAsync(dbContext, _ct);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(secondId, first.BlockId, first.RowId), _ct);
        var handler = new GetContainerStaysHandler(dbContext);

        var all = await handler.HandleAsync(new GetContainerStaysQuery(), _ct);
        var inYard = await handler.HandleAsync(new GetContainerStaysQuery(InYard: true), _ct);

        Assert.Equal(2, all.Value.Count);
        Assert.Equal([secondId], inYard.Value.Select(x => x.ContainerId));
    }
}
