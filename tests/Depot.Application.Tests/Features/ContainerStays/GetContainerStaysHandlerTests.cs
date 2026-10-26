using Depot.Application.Features.ContainerStays.GetAll;
using Depot.Application.Features.ContainerStays.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.ContainerStays;

public sealed class GetContainerStaysHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoStayExists()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetContainerStaysHandler(dbContext).HandleAsync(new GetContainerStaysQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsStaysNewestFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var secondId = await GateInTestData.CreateSecondContainerAsync(dbContext, _ct);
        var handler = GateInTestData.CreateHandler(dbContext);
        await handler.HandleAsync(
            GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId, gateInAt: _now.AddHours(-5)),
            _ct);
        await handler.HandleAsync(
            GateInTestData.Command(secondId, yard.BlockId, yard.RowId, 2, _now.AddHours(-1)),
            _ct);

        var result = await new GetContainerStaysHandler(dbContext).HandleAsync(new GetContainerStaysQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal([secondId, yard.ContainerId], result.Value.Select(x => x.ContainerId));
    }

    [Fact]
    public async Task GetById_ReturnsTheStay()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var created = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), _ct);

        var result = await new GetContainerStayByIdHandler(dbContext)
            .HandleAsync(new GetContainerStayByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(yard.ContainerId, result.Value.ContainerId);
        Assert.Equal(yard.RowId, result.Value.RowId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdIsUnknown()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetContainerStayByIdHandler(dbContext)
            .HandleAsync(new GetContainerStayByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerStay.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
