using Depot.Application.Features.Blocks.Create;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class CreateBlockHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesBlock_WhenInputIsValid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateBlockHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBlockCommand(" a1 ", "Khu A1"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("A1", result.Value.Code);
        Assert.False(result.Value.IsVirtual);
        Assert.Equal(1, await dbContext.Blocks.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_SavesVirtualBlock_WhenIsVirtualIsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateBlockHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBlockCommand("V1", "Virtual 1", true), _ct);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsVirtual);
        Assert.True((await dbContext.Blocks.SingleAsync(_ct)).IsVirtual);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenCodeAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateBlockHandler(dbContext);
        await handler.HandleAsync(new CreateBlockCommand("A1", "Khu A1"), _ct);

        var result = await handler.HandleAsync(new CreateBlockCommand(" a1 ", "Another"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.CodeExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.Blocks.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenCodeIsEmpty()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateBlockHandler(dbContext);

        var result = await handler.HandleAsync(new CreateBlockCommand("  ", "Khu A"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Code.Empty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Blocks.CountAsync(_ct));
    }
}
