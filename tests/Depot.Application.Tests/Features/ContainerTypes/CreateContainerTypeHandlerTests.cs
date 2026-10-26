using Depot.Application.Features.ContainerTypes.Create;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerTypes;

public sealed class CreateContainerTypeHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesContainerType_WhenInputIsValid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateContainerTypeHandler(dbContext);

        var result = await handler.HandleAsync(new CreateContainerTypeCommand(" dry ", "Dry Container"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("DRY", result.Value.Code);
        Assert.Equal("Dry Container", result.Value.Name);
        Assert.Equal(1, await dbContext.ContainerTypes.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenCodeAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateContainerTypeHandler(dbContext);
        await handler.HandleAsync(new CreateContainerTypeCommand("DRY", "Dry Container"), _ct);

        var result = await handler.HandleAsync(new CreateContainerTypeCommand(" dry ", "Another Name"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerType.CodeExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.ContainerTypes.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenCodeIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateContainerTypeHandler(dbContext);

        var result = await handler.HandleAsync(new CreateContainerTypeCommand("D", "Dry Container"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Code.TooShort", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.ContainerTypes.CountAsync(_ct));
    }
}
