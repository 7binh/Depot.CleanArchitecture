using Depot.Application.Features.ReleaseOrders.GetDeliveries;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Domain.Gate;

namespace Depot.Application.Tests.Features.ReleaseOrders;

public sealed class GetReleaseOrderDeliveriesHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheOrderIsUnknown()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetReleaseOrderDeliveriesHandler(dbContext)
            .HandleAsync(new GetReleaseOrderDeliveriesQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmpty_WhenNothingHasLeftOnTheOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var result = await new GetReleaseOrderDeliveriesHandler(dbContext)
            .HandleAsync(new GetReleaseOrderDeliveriesQuery(orderId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ListsTheContainerThatLeftOnThisOrder_WithItsLineAndVehicle()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        var result = await new GetReleaseOrderDeliveriesHandler(dbContext)
            .HandleAsync(new GetReleaseOrderDeliveriesQuery(orderId), _ct);

        var delivery = Assert.Single(result.Value);
        Assert.Equal(yard.ContainerId, delivery.ContainerId);
        Assert.Equal(ContainerTestData.ValidNumber, delivery.ContainerNumber);
        Assert.Equal(20, delivery.SizeFeet);
        Assert.Equal(VehicleType.Truck, delivery.OutVehicleType);
        Assert.Equal("51C-99999", delivery.OutVehicleNumber);
    }

    [Fact]
    public async Task HandleAsync_LeavesOutContainersThatLeftOnAnotherOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var other = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct, orderNo: "RO-OTHER");
        var mine = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct, orderNo: "RO-MINE");
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, other), _ct);

        var result = await new GetReleaseOrderDeliveriesHandler(dbContext)
            .HandleAsync(new GetReleaseOrderDeliveriesQuery(mine), _ct);

        Assert.Empty(result.Value);
    }
}
