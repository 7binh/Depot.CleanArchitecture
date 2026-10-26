using Depot.Application.Features.LineOperators.Create;
using Depot.Application.Features.LineOperators.GetAll;

namespace Depot.Application.Tests.Features.LineOperators;

public sealed class GetLineOperatorsHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoLineOperatorExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetLineOperatorsHandler(dbContext);

        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsLineOperatorsOrderedByCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var create = new CreateLineOperatorHandler(dbContext);
        await create.HandleAsync(new CreateLineOperatorCommand("MSC", "Mediterranean Shipping"), _ct);
        await create.HandleAsync(new CreateLineOperatorCommand("CMA", "CMA CGM"), _ct);
        await create.HandleAsync(new CreateLineOperatorCommand("ONE", "Ocean Network Express"), _ct);
        var handler = new GetLineOperatorsHandler(dbContext);

        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(["CMA", "MSC", "ONE"], result.Value.Select(x => x.Code));
    }
}
