using Depot.Application.Features.ContainerTypes.Create;
using Depot.Application.Features.ContainerTypes.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.ContainerTypes;

public sealed class GetContainerTypeByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsContainerType_WhenIdExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateContainerTypeHandler(dbContext)
            .HandleAsync(new CreateContainerTypeCommand("DRY", "Dry Container"), _ct);
        var handler = new GetContainerTypeByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerTypeByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value.Id);
        Assert.Equal("DRY", result.Value.Code);
        Assert.Equal("Dry Container", result.Value.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetContainerTypeByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerTypeByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerType.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
