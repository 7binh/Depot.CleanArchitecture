using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.Update;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Blocks;

public sealed class UpdateBlockHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_RenamesTheBlock_AndKeepsTheCodeAndVirtualFlag()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("v1", "Khu cu", true), _ct);

        var result = await new UpdateBlockHandler(dbContext)
            .HandleAsync(new UpdateBlockCommand(created.Value.Id, "  Khu moi "), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Khu moi", result.Value.Name);
        Assert.Equal("V1", result.Value.Code);
        Assert.True(result.Value.IsVirtual);
        Assert.Equal("Khu moi", (await dbContext.Blocks.SingleAsync(_ct)).Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheBlockDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new UpdateBlockHandler(dbContext).HandleAsync(new UpdateBlockCommand(Guid.NewGuid(), "X"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Block.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndKeepsTheName_WhenTheNameIsEmpty()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("A", "Khu A", false), _ct);

        var result = await new UpdateBlockHandler(dbContext).HandleAsync(new UpdateBlockCommand(created.Value.Id, " "), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Name.Empty", result.Error.Code);
        Assert.Equal("Khu A", (await dbContext.Blocks.SingleAsync(_ct)).Name);
    }
}
