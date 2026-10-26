using Depot.Application.Features.YardRows.Create;
using Depot.Application.Features.YardRows.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.YardRows;

public sealed class GetYardRowByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsRow_WhenIdBelongsToTheBay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var row = await new CreateYardRowHandler(dbContext)
            .HandleAsync(new CreateYardRowCommand(blockId, bayId, 2, 5), _ct);
        var handler = new GetYardRowByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowByIdQuery(blockId, bayId, row.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(row.Value.Id, result.Value.Id);
        Assert.Equal(bayId, result.Value.BayId);
        Assert.Equal(2, result.Value.RowNo);
        Assert.Equal(5, result.Value.MaxTier);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRowDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var handler = new GetYardRowByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowByIdQuery(blockId, bayId, Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("YardRow.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRowBelongsToAnotherBay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bay1 = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var bay2 = await YardTestData.CreateBayAsync(dbContext, blockId, 2, _ct);
        var row = await new CreateYardRowHandler(dbContext)
            .HandleAsync(new CreateYardRowCommand(blockId, bay1, 1, 3), _ct);
        var handler = new GetYardRowByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetYardRowByIdQuery(blockId, bay2, row.Value.Id), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("YardRow.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBayNotFound_WhenBayDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetYardRowByIdHandler(dbContext);

        var result = await handler.HandleAsync(
            new GetYardRowByIdQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
    }
}
