using Depot.Application.Features.Bays.Create;
using Depot.Application.Features.Blocks.Create;
using Depot.Domain.Common;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Bays;

public sealed class CreateBayHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static async Task<Guid> CreateBlockAsync(AppDbContext dbContext, bool isVirtual)
    {
        var block = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand("A", "Khu A", isVirtual), _ct);
        return block.Value.Id;
    }

    [Fact]
    public async Task HandleAsync_SavesBay_WhenBlockIsNormal()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, false);
        var handler = new CreateBayHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBayCommand(blockId, 1), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(blockId, result.Value.BlockId);
        Assert.Equal(1, result.Value.BayNo);
        Assert.Equal(1, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateBayHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBayCommand(Guid.NewGuid(), 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenBlockIsVirtual()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, true);
        var handler = new CreateBayHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBayCommand(blockId, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.BlockIsVirtual", result.Error.Code);
        Assert.Equal(0, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenBayNoAlreadyExistsInTheSameBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, false);
        var handler = new CreateBayHandler(dbContext);
        await handler.HandleAsync(new CreateBayCommand(blockId, 1), _ct);

        var result = await handler.HandleAsync(new CreateBayCommand(blockId, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.BayNoExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_AllowsSameBayNo_InDifferentBlocks()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var createBlock = new CreateBlockHandler(dbContext);
        var blockA = (await createBlock.HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct)).Value.Id;
        var blockB = (await createBlock.HandleAsync(new CreateBlockCommand("B", "Khu B"), _ct)).Value.Id;
        var handler = new CreateBayHandler(dbContext);
        await handler.HandleAsync(new CreateBayCommand(blockA, 1), _ct);

        var result = await handler.HandleAsync(new CreateBayCommand(blockB, 1), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenBayNoIsOutOfRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, false);
        var handler = new CreateBayHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBayCommand(blockId, 0), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("BayNo.Invalid", result.Error.Code);
        Assert.Equal(0, await dbContext.Bays.CountAsync(_ct));
    }
}
