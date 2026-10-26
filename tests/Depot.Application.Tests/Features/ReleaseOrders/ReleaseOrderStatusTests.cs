using Depot.Application.Features.ReleaseOrders;
using Depot.Application.Features.ReleaseOrders.GetAll;
using Depot.Application.Features.ReleaseOrders.GetById;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Domain.ReleaseOrders;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.ReleaseOrders;

public sealed class ReleaseOrderStatusTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetById_IsNotDelivered_BeforeAnyContainerLeaves()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await GateInTestData.CreateYardAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);

        var order = (await GetByIdAsync(dbContext, orderId)).Value;

        Assert.Equal(ReleaseOrderStatus.NotDelivered, order.Status);
        var item = Assert.Single(order.Items);
        Assert.Equal(0, item.Delivered);
        Assert.Equal(2, item.Remaining);
    }

    [Fact]
    public async Task GetById_IsDelivering_WhenSomeButNotAllContainersLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 2, _ct);
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);

        var order = (await GetByIdAsync(dbContext, orderId)).Value;

        Assert.Equal(ReleaseOrderStatus.Delivering, order.Status);
        var item = Assert.Single(order.Items);
        Assert.Equal(1, item.Delivered);
        Assert.Equal(1, item.Remaining);
    }

    [Fact]
    public async Task GetAll_IsDelivered_WhenEveryLineIsFull_AndKeepsOtherOrdersApart()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var fullId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        var untouchedId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct, orderNo: "RO-002");
        await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, fullId), _ct);

        var result = await new GetReleaseOrdersHandler(dbContext, ContainerTestData.Clock)
            .HandleAsync(new GetReleaseOrdersQuery(), _ct);

        var byId = result.Value.ToDictionary(x => x.Id);
        Assert.Equal(ReleaseOrderStatus.Delivered, byId[fullId].Status);
        Assert.Equal(ReleaseOrderStatus.NotDelivered, byId[untouchedId].Status);
    }

    private static Task<Result<ReleaseOrderResponse>> GetByIdAsync(AppDbContext dbContext, Guid id)
    {
        return new GetReleaseOrderByIdHandler(dbContext, ContainerTestData.Clock)
            .HandleAsync(new GetReleaseOrderByIdQuery(id), _ct);
    }
}
