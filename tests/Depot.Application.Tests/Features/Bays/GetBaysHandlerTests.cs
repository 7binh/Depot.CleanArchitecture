using Depot.Application.Features.Bays.Create;
using Depot.Application.Features.Bays.GetAll;
using Depot.Application.Features.Blocks.Create;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Bays;

public sealed class GetBaysHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetBaysHandler(dbContext);

        var result = await handler.HandleAsync(new GetBaysQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenBlockHasNoBay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var block = await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct);
        var handler = new GetBaysHandler(dbContext);

        var result = await handler.HandleAsync(new GetBaysQuery(block.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyBaysOfThatBlock_OrderedByBayNo()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var createBlock = new CreateBlockHandler(dbContext);
        var blockA = (await createBlock.HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct)).Value.Id;
        var blockB = (await createBlock.HandleAsync(new CreateBlockCommand("B", "Khu B"), _ct)).Value.Id;
        var create = new CreateBayHandler(dbContext);
        await create.HandleAsync(new CreateBayCommand(blockA, 3), _ct);
        await create.HandleAsync(new CreateBayCommand(blockA, 1), _ct);
        await create.HandleAsync(new CreateBayCommand(blockA, 2), _ct);
        await create.HandleAsync(new CreateBayCommand(blockB, 9), _ct);
        var handler = new GetBaysHandler(dbContext);

        var result = await handler.HandleAsync(new GetBaysQuery(blockA), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3], result.Value.Select(x => x.BayNo));
        Assert.All(result.Value, x => Assert.Equal(blockA, x.BlockId));
    }
}
