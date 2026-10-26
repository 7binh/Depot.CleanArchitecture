using Depot.Application.Features.LineOperators.Create;
using Depot.Application.Features.LineOperators.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.LineOperators;

public sealed class GetLineOperatorByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsLineOperator_WhenIdExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateLineOperatorHandler(dbContext)
            .HandleAsync(new CreateLineOperatorCommand("MSC", "Mediterranean Shipping"), _ct);
        var handler = new GetLineOperatorByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetLineOperatorByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value.Id);
        Assert.Equal("MSC", result.Value.Code);
        Assert.Equal("Mediterranean Shipping", result.Value.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetLineOperatorByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetLineOperatorByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("LineOperator.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
