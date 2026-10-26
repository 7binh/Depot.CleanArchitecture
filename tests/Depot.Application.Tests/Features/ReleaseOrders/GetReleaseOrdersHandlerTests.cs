using Depot.Application.Features.ReleaseOrders.GetAll;
using Depot.Application.Features.ReleaseOrders.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.ReleaseOrders;

public sealed class GetReleaseOrdersHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAll_ReturnsOrdersWithLines_AndFlagsExpiredOnes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var create = ReleaseOrderTestData.CreateHandler(dbContext);
        await create.HandleAsync(ReleaseOrderTestData.Command(refs, "RO-LIVE"), _ct);
        await create.HandleAsync(
            ReleaseOrderTestData.Command(refs, "RO-TODAY", expiryDate: ContainerTestData.Today),
            _ct);
        var expired = ReleaseOrderTestData.Command(refs, "RO-OLD") with
        {
            IssuedDate = ContainerTestData.Today.AddDays(-10),
            ExpiryDate = ContainerTestData.Today.AddDays(-1)
        };
        await create.HandleAsync(expired, _ct);

        var result = await new GetReleaseOrdersHandler(dbContext, ContainerTestData.Clock)
            .HandleAsync(new GetReleaseOrdersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(["RO-LIVE", "RO-TODAY", "RO-OLD"], result.Value.Select(x => x.OrderNo));
        Assert.Equal([false, false, true], result.Value.Select(x => x.IsExpired));
        Assert.All(result.Value, order => Assert.Single(order.Items));
    }

    [Fact]
    public async Task GetById_ReturnsTheOrderWithItsLines()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var created = await ReleaseOrderTestData.CreateHandler(dbContext)
            .HandleAsync(ReleaseOrderTestData.Command(refs), _ct);

        var result = await new GetReleaseOrderByIdHandler(dbContext, ContainerTestData.Clock)
            .HandleAsync(new GetReleaseOrderByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("RO-001", result.Value.OrderNo);
        Assert.Equal(refs.CustomerId, result.Value.CustomerId);
        Assert.Equal(5, Assert.Single(result.Value.Items).Quantity);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdIsUnknown()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetReleaseOrderByIdHandler(dbContext, ContainerTestData.Clock)
            .HandleAsync(new GetReleaseOrderByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
