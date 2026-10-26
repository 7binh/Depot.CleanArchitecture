using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GenerateLayout;
using Depot.Application.Features.YardRows.Create;
using Depot.Domain.Common;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class GenerateLayoutHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static async Task<Guid> CreateBlockAsync(AppDbContext dbContext, string code = "A", bool isVirtual = false)
    {
        var block = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand(code, $"Khu {code}", isVirtual), _ct);
        return block.Value.Id;
    }

    private static Task<Result<GenerateLayoutResponse>> GenerateAsync(
        AppDbContext dbContext,
        Guid blockId,
        int fromBay = 1,
        int toBay = 4,
        int rowCount = 3,
        int maxTier = 4)
    {
        return new GenerateLayoutHandler(dbContext)
            .HandleAsync(new GenerateLayoutCommand(blockId, fromBay, toBay, rowCount, maxTier), _ct);
    }

    [Fact]
    public async Task HandleAsync_CreatesEveryBayWithItsRows()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext);

        var result = await GenerateAsync(dbContext, blockId, 1, 4, 3, 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.BaysCreated);
        Assert.Equal(12, result.Value.RowsCreated);
        Assert.Equal([1, 2, 3, 4], await dbContext.Bays.OrderBy(x => x.BayNo).Select(x => x.BayNo).ToListAsync(_ct));
        Assert.Equal(12, await dbContext.YardRows.CountAsync(_ct));
        Assert.All(await dbContext.YardRows.ToListAsync(_ct), row => Assert.Equal(5, row.MaxTier));
    }

    [Fact]
    public async Task HandleAsync_KeepsExistingBaysAndRows_AndAddsOnlyTheMissingOnes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 2, _ct);
        await new CreateYardRowHandler(dbContext).HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 2), _ct);

        var result = await GenerateAsync(dbContext, blockId, 1, 3, 2, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.BaysCreated);
        Assert.Equal(5, result.Value.RowsCreated);
        Assert.Equal(3, await dbContext.Bays.CountAsync(_ct));
        Assert.Equal(2, (await dbContext.YardRows.SingleAsync(x => x.BayId == bayId && x.RowNo == 1, _ct)).MaxTier);
    }

    [Fact]
    public async Task HandleAsync_CreatesNothing_WhenCalledTwiceWithTheSameInput()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext);
        await GenerateAsync(dbContext, blockId);

        var result = await GenerateAsync(dbContext, blockId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.BaysCreated);
        Assert.Equal(0, result.Value.RowsCreated);
    }

    [Fact]
    public async Task HandleAsync_LeavesOtherBlocksAlone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockA = await CreateBlockAsync(dbContext, "A");
        var blockB = await CreateBlockAsync(dbContext, "B");
        await GenerateAsync(dbContext, blockA, 1, 2);

        var result = await GenerateAsync(dbContext, blockB, 1, 2);

        Assert.Equal(2, result.Value.BaysCreated);
        Assert.Equal(4, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await GenerateAsync(dbContext, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenTheBlockIsVirtual()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext, isVirtual: true);

        var result = await GenerateAsync(dbContext, blockId);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.BlockIsVirtual", result.Error.Code);
        Assert.Equal(0, await dbContext.Bays.CountAsync(_ct));
    }

    [Theory]
    [InlineData(0, 3, 2, 4, "BayRange.Invalid")]
    [InlineData(4, 3, 2, 4, "BayRange.Invalid")]
    [InlineData(1, 1000, 2, 4, "BayRange.Invalid")]
    [InlineData(1, 3, 0, 4, "RowCount.Invalid")]
    [InlineData(1, 3, 100, 4, "RowCount.Invalid")]
    [InlineData(1, 3, 2, 1, "MaxTier.Invalid")]
    [InlineData(1, 3, 2, 6, "MaxTier.Invalid")]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenTheInputIsOutOfRange(
        int fromBay,
        int toBay,
        int rowCount,
        int maxTier,
        string code)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockId = await CreateBlockAsync(dbContext);

        var result = await GenerateAsync(dbContext, blockId, fromBay, toBay, rowCount, maxTier);

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Bays.CountAsync(_ct));
        Assert.Equal(0, await dbContext.YardRows.CountAsync(_ct));
    }
}
