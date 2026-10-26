using Depot.Application.Features.Containers.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Containers;

public sealed class GetContainerByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsContainer_WhenItExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var operatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var created = await ContainerTestData.CreateHandler(dbContext)
            .HandleAsync(ContainerTestData.Command(typeId), _ct);
        var handler = new GetContainerByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value.Id);
        Assert.Equal(ContainerTestData.ValidNumber, result.Value.Number);
        Assert.Equal(operatorId, result.Value.LineOperatorId);
        Assert.Equal(30480, result.Value.MaxWeightKg);
        Assert.Equal(2200, result.Value.TareWeightKg);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenContainerDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetContainerByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetContainerByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
