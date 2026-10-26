using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class GetBlockByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsBlock_WhenIdExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand("V1", "Virtual 1", true), _ct);
        var handler = new GetBlockByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetBlockByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value.Id);
        Assert.Equal("V1", result.Value.Code);
        Assert.Equal("Virtual 1", result.Value.Name);
        Assert.True(result.Value.IsVirtual);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetBlockByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetBlockByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
