using Depot.Application.Features.ContainerTypes.Create;
using Depot.Application.Features.ContainerTypes.GetAll;

namespace Depot.Application.Tests.Features.ContainerTypes;

public sealed class GetContainerTypesHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoContainerTypeExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetContainerTypesHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerTypesQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsContainerTypesOrderedByCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var create = new CreateContainerTypeHandler(dbContext);
        await create.HandleAsync(new CreateContainerTypeCommand("REEFER", "Reefer"), _ct);
        await create.HandleAsync(new CreateContainerTypeCommand("DRY", "Dry"), _ct);
        await create.HandleAsync(new CreateContainerTypeCommand("OPENTOP", "Open Top"), _ct);
        var handler = new GetContainerTypesHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerTypesQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(["DRY", "OPENTOP", "REEFER"], result.Value.Select(x => x.Code));
    }
}
