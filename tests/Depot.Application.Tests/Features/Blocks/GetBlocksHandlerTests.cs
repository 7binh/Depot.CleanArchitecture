using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GetAll;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class GetBlocksHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoBlockExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetBlocksHandler(dbContext);

        var result = await handler.HandleAsync(new GetBlocksQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBlocksOrderedByCode_WithVirtualFlag()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var create = new CreateBlockHandler(dbContext);
        await create.HandleAsync(new CreateBlockCommand("C", "Khu C"), _ct);
        await create.HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct);
        await create.HandleAsync(new CreateBlockCommand("B", "Khu B", true), _ct);
        var handler = new GetBlocksHandler(dbContext);

        var result = await handler.HandleAsync(new GetBlocksQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(["A", "B", "C"], result.Value.Select(x => x.Code));
        Assert.Equal([false, true, false], result.Value.Select(x => x.IsVirtual));
    }
}
