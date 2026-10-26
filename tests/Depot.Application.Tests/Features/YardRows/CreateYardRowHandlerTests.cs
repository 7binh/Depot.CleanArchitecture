using Depot.Application.Features.YardRows.Create;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.YardRows;

public sealed class CreateYardRowHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesRow_WhenBayExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var handler = new CreateYardRowHandler(dbContext);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 4), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(bayId, result.Value.BayId);
        Assert.Equal(1, result.Value.RowNo);
        Assert.Equal(4, result.Value.MaxTier);
        Assert.Equal(1, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBayDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var handler = new CreateYardRowHandler(dbContext);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockId, Guid.NewGuid(), 1, 4), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBayBelongsToAnotherBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockA = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var blockB = await YardTestData.CreateBlockAsync(dbContext, "B", _ct);
        var bayOfA = await YardTestData.CreateBayAsync(dbContext, blockA, 1, _ct);
        var handler = new CreateYardRowHandler(dbContext);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockB, bayOfA, 1, 4), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
        Assert.Equal(0, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenRowNoAlreadyExistsInTheSameBay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var handler = new CreateYardRowHandler(dbContext);
        await handler.HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 4), _ct);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 5), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("YardRow.RowNoExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_AllowsSameRowNo_InDifferentBays()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bay1 = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var bay2 = await YardTestData.CreateBayAsync(dbContext, blockId, 2, _ct);
        var handler = new CreateYardRowHandler(dbContext);
        await handler.HandleAsync(new CreateYardRowCommand(blockId, bay1, 1, 4), _ct);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockId, bay2, 1, 4), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenMaxTierIsBelowTwo()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", _ct);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, _ct);
        var handler = new CreateYardRowHandler(dbContext);

        var result = await handler.HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("MaxTier.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.YardRows.CountAsync(_ct));
    }
}
