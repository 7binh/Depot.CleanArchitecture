using Depot.Application.Features.Bays.Create;
using Depot.Application.Features.Bays.GetById;
using Depot.Application.Features.Blocks.Create;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Bays;

public sealed class GetBayByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsBay_WhenIdBelongsToTheBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var block = await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct);
        var bay = await new CreateBayHandler(dbContext).HandleAsync(new CreateBayCommand(block.Value.Id, 4), _ct);
        var handler = new GetBayByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetBayByIdQuery(block.Value.Id, bay.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(bay.Value.Id, result.Value.Id);
        Assert.Equal(block.Value.Id, result.Value.BlockId);
        Assert.Equal(4, result.Value.BayNo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetBayByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetBayByIdQuery(Guid.NewGuid(), Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenBayBelongsToAnotherBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var createBlock = new CreateBlockHandler(dbContext);
        var blockA = (await createBlock.HandleAsync(new CreateBlockCommand("A", "Khu A"), _ct)).Value.Id;
        var blockB = (await createBlock.HandleAsync(new CreateBlockCommand("B", "Khu B"), _ct)).Value.Id;
        var bay = await new CreateBayHandler(dbContext).HandleAsync(new CreateBayCommand(blockA, 1), _ct);
        var handler = new GetBayByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetBayByIdQuery(blockB, bay.Value.Id), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
    }
}
