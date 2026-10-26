using Depot.Application.Features.YardRows.Delete;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.YardRows;

public sealed class DeleteYardRowHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private sealed record Yard(Guid BlockId, Guid BayId, Guid RowId, Guid ContainerId);

    // One 20ft container, and block A with bay 1 and a 4-tier row.
    private static async Task<Yard> CreateYardAsync(Depot.Infrastructure.Persistence.AppDbContext dbContext)
    {
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var bay = await dbContext.Bays.SingleAsync(_ct);
        return new Yard(yard.BlockId, bay.Id, yard.RowId, yard.ContainerId);
    }

    private static Task<Result> DeleteAsync(Depot.Infrastructure.Persistence.AppDbContext dbContext, Yard yard)
    {
        return new DeleteYardRowHandler(dbContext)
            .HandleAsync(new DeleteYardRowCommand(yard.BlockId, yard.BayId, yard.RowId), _ct);
    }

    [Fact]
    public async Task HandleAsync_RemovesAnEmptyRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);

        var result = await DeleteAsync(dbContext, yard);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await dbContext.YardRows.CountAsync(_ct));
        Assert.Equal(1, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_RefusesARowThatHoldsAContainer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        var result = await DeleteAsync(dbContext, yard);

        Assert.True(result.IsFailure);
        Assert.Equal("Row.HasContainers", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ForgetsThePositionOfContainersThatAlreadyLeft_AndKeepsTheirHistory()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        await GateOutTestData.CreateHandler(dbContext).HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);
        var bayId = await dbContext.Bays.Select(x => x.Id).SingleAsync(_ct);

        var result = await new DeleteYardRowHandler(dbContext)
            .HandleAsync(new DeleteYardRowCommand(yard.BlockId, bayId, yard.RowId), _ct);

        Assert.True(result.IsSuccess);
        var stay = await dbContext.ContainerStays.SingleAsync(_ct);
        Assert.Null(stay.RowId);
        Assert.NotNull(stay.GateOutAt);
        Assert.Equal(yard.ContainerId, stay.ContainerId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheRowOrBayDoesNotBelongTogether()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var handler = new DeleteYardRowHandler(dbContext);

        var wrongRow = await handler.HandleAsync(new DeleteYardRowCommand(yard.BlockId, yard.BayId, Guid.NewGuid()), _ct);
        var wrongBay = await handler.HandleAsync(new DeleteYardRowCommand(yard.BlockId, Guid.NewGuid(), yard.RowId), _ct);

        Assert.True(wrongRow.IsFailure);
        Assert.True(wrongBay.IsFailure);
        Assert.Equal("YardRow.NotFound", wrongRow.Error.Code);
        Assert.Equal("Bay.NotFound", wrongBay.Error.Code);
    }
}
