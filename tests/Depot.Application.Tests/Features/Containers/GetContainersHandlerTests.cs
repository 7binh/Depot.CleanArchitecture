using Depot.Application.Features.Containers.GetAll;

namespace Depot.Application.Tests.Features.Containers;

public sealed class GetContainersHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoContainerExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetContainersHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsContainersOrderedByNumber_WithAllFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        await ContainerTestData.CreateOperatorAsync(dbContext, "ABC", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var create = ContainerTestData.CreateHandler(dbContext);
        // ABCU1234560: ISO 6346 check digit of ABCU123456 is 0 (sum 5478, remainder 0).
        await create.HandleAsync(ContainerTestData.Command(typeId), _ct);
        await create.HandleAsync(ContainerTestData.Command(typeId, "ABCU1234560"), _ct);
        var handler = new GetContainersHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(["ABCU1234560", ContainerTestData.ValidNumber], result.Value.Select(x => x.Number));
        Assert.All(result.Value, x => Assert.Equal(typeId, x.ContainerTypeId));
        Assert.All(result.Value, x => Assert.Equal(new DateOnly(2020, 5, 1), x.ManufactureDate));
    }
}
