using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.Delete;
using Depot.Application.Features.Blocks.GenerateLayout;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class DeleteBlockHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_RemovesTheBlockWithItsBaysAndRows_ButLeavesOtherBlocks()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blockA = (await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("A", "Khu A", false), _ct)).Value.Id;
        var blockB = (await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("B", "Khu B", false), _ct)).Value.Id;
        var layout = new GenerateLayoutHandler(dbContext);
        await layout.HandleAsync(new GenerateLayoutCommand(blockA, 1, 3, 2, 4), _ct);
        await layout.HandleAsync(new GenerateLayoutCommand(blockB, 1, 2, 2, 4), _ct);

        var result = await new DeleteBlockHandler(dbContext).HandleAsync(new DeleteBlockCommand(blockA), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(blockB, (await dbContext.Blocks.SingleAsync(_ct)).Id);
        Assert.Equal(2, await dbContext.Bays.CountAsync(_ct));
        Assert.Equal(4, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new DeleteBlockHandler(dbContext).HandleAsync(new DeleteBlockCommand(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_RefusesABlockThatHoldsAContainer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);

        var result = await new DeleteBlockHandler(dbContext).HandleAsync(new DeleteBlockCommand(yard.BlockId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.InUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.Blocks.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_RefusesABlockThatOnlyHasHistory()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        var left = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);
        Assert.True(left.IsSuccess);

        var result = await new DeleteBlockHandler(dbContext).HandleAsync(new DeleteBlockCommand(yard.BlockId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.InUse", result.Error.Code);
    }
}
