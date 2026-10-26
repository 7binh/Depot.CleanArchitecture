using Depot.Application.Features.YardRows.Create;
using Depot.Application.Features.YardRows.GetAll;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.YardRows;

public sealed class GetYardRowsHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBayDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetYardRowsHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowsQuery(Guid.NewGuid(), Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenBayHasNoRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var handler = new GetYardRowsHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowsQuery(blockId, bayId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyRowsOfThatBay_OrderedByRowNo_WithMaxTier()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bay1 = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var bay2 = await YardTestData.CreateBayAsync(dbContext, blockId, 2, _ct);
        var create = new CreateYardRowHandler(dbContext);
        await create.HandleAsync(new CreateYardRowCommand(blockId, bay1, 3, 5), _ct);
        await create.HandleAsync(new CreateYardRowCommand(blockId, bay1, 1, 3), _ct);
        await create.HandleAsync(new CreateYardRowCommand(blockId, bay1, 2, 4), _ct);
        await create.HandleAsync(new CreateYardRowCommand(blockId, bay2, 9, 2), _ct);
        var handler = new GetYardRowsHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowsQuery(blockId, bay1), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3], result.Value.Select(x => x.RowNo));
        Assert.Equal([3, 4, 5], result.Value.Select(x => x.MaxTier));
        Assert.All(result.Value, x => Assert.Equal(bay1, x.BayId));
    }
}
