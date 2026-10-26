using Depot.Application.Features.ReleaseOrders.Create;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ReleaseOrders;

public sealed class CreateReleaseOrderHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesOrderWithItsLines()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var lines = new[] { new ReleaseOrderLineRequest(refs.TypeId, 40, 2), new ReleaseOrderLineRequest(refs.TypeId, 20, 5) };

        var result = await ReleaseOrderTestData.CreateHandler(dbContext)
            .HandleAsync(ReleaseOrderTestData.Command(refs, lines: lines), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("RO-001", result.Value.OrderNo);
        Assert.False(result.Value.IsExpired);
        Assert.Equal([20, 40], result.Value.Items.Select(x => x.SizeFeet));
        Assert.Equal(1, await dbContext.ReleaseOrders.CountAsync(_ct));
        Assert.Equal(2, (await dbContext.ReleaseOrders.Include(x => x.Items).SingleAsync(_ct)).Items.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenOperatorDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var command = ReleaseOrderTestData.Command(refs) with { LineOperatorId = Guid.NewGuid() };

        var result = await ReleaseOrderTestData.CreateHandler(dbContext).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.OperatorNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenCustomerDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var command = ReleaseOrderTestData.Command(refs) with { CustomerId = Guid.NewGuid() };

        var result = await ReleaseOrderTestData.CreateHandler(dbContext).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.CustomerNotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenAContainerTypeDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var lines = new[] { new ReleaseOrderLineRequest(Guid.NewGuid(), 20, 1) };

        var result = await ReleaseOrderTestData.CreateHandler(dbContext)
            .HandleAsync(ReleaseOrderTestData.Command(refs, lines: lines), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.TypeNotFound", result.Error.Code);
        Assert.Equal(0, await dbContext.ReleaseOrders.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_WhenLinesAreMissing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var command = ReleaseOrderTestData.Command(refs) with { Lines = null };

        var result = await ReleaseOrderTestData.CreateHandler(dbContext).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Lines.Empty", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_AndSavesNothing_WhenExpiryIsBeforeIssuedDate()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var command = ReleaseOrderTestData.Command(refs, expiryDate: ContainerTestData.Today.AddDays(-1));

        var result = await ReleaseOrderTestData.CreateHandler(dbContext).HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiryDate.BeforeIssuedDate", result.Error.Code);
        Assert.Equal(0, await dbContext.ReleaseOrders.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenOrderNoExistsForTheSameOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var handler = ReleaseOrderTestData.CreateHandler(dbContext);
        await handler.HandleAsync(ReleaseOrderTestData.Command(refs), _ct);

        var result = await handler.HandleAsync(ReleaseOrderTestData.Command(refs), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReleaseOrder.OrderNoExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.ReleaseOrders.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_AllowsTheSameOrderNo_ForAnotherOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var refs = await ReleaseOrderTestData.CreateRefsAsync(dbContext, _ct);
        var otherOperatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "MSC", _ct);
        var handler = ReleaseOrderTestData.CreateHandler(dbContext);
        await handler.HandleAsync(ReleaseOrderTestData.Command(refs), _ct);

        var result = await handler.HandleAsync(
            ReleaseOrderTestData.Command(refs) with { LineOperatorId = otherOperatorId },
            _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, await dbContext.ReleaseOrders.CountAsync(_ct));
    }
}
