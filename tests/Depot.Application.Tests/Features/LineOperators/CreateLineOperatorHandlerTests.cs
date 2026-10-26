using Depot.Application.Features.LineOperators.Create;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.LineOperators;

public sealed class CreateLineOperatorHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesLineOperator_WhenInputIsValid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateLineOperatorHandler(dbContext);

        var result = await handler.HandleAsync(new CreateLineOperatorCommand(" msc ", "Mediterranean Shipping"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("MSC", result.Value.Code);
        Assert.Equal("Mediterranean Shipping", result.Value.Name);
        Assert.Equal(1, await dbContext.LineOperators.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenCodeAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateLineOperatorHandler(dbContext);
        await handler.HandleAsync(new CreateLineOperatorCommand("MSC", "Mediterranean Shipping"), _ct);

        var result = await handler.HandleAsync(new CreateLineOperatorCommand(" msc ", "Another Name"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("LineOperator.CodeExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.LineOperators.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenAnotherRequestSavesSameCodeFirst()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var inner = TestDbContextFactory.Create(databaseName);
        await using var rival = TestDbContextFactory.Create(databaseName);
        var rivalEntity = LineOperator.Create("MSC", "Saved By Rival").Value;
        var handler = new CreateLineOperatorHandler(new RacingDbContext(inner, async ct =>
        {
            rival.LineOperators.Add(rivalEntity);
            await rival.SaveChangesAsync(ct);
        }));

        var result = await handler.HandleAsync(new CreateLineOperatorCommand("MSC", "Mediterranean Shipping"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("LineOperator.CodeExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenCodeIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateLineOperatorHandler(dbContext);

        var result = await handler.HandleAsync(new CreateLineOperatorCommand("MS", "Maersk"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Code.InvalidLength", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.LineOperators.CountAsync(_ct));
    }
}
