using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Containers;

public sealed class CreateContainerHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesContainer_AndTakesOperatorFromTheOwnerCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var operatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContainerTestData.ValidNumber, result.Value.Number);
        Assert.Equal(operatorId, result.Value.LineOperatorId);
        Assert.Equal(typeId, result.Value.ContainerTypeId);
        Assert.Equal("22G1", result.Value.IsoCode);
        Assert.Equal(20, result.Value.SizeFeet);
        Assert.Equal(1, await dbContext.Containers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_TrimsAndUppercasesNumber_BeforeLookingUpTheOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId, "  csqu3054383 "), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContainerTestData.ValidNumber, result.Value.Number);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CS")]
    public async Task HandleAsync_ReturnsValidationError_WhenNumberHasNoOwnerCode(string? number)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId, number), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerNumber.InvalidFormat", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Containers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenOwnerCodeIsNotAnOperator()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.OperatorNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Containers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_WhenContainerTypeDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.TypeNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Containers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsDomainError_AndSavesNothing_WhenCheckDigitIsWrong()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId, "CSQU3054384"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ContainerNumber.InvalidCheckDigit", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Containers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_UsesTheInjectedClock_ToRejectAFutureManufactureDate()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);
        var command = ContainerTestData.Command(typeId, manufactureDate: ContainerTestData.Today.AddDays(1));

        var result = await handler.HandleAsync(command, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ManufactureDate.InFuture", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenNumberAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var handler = ContainerTestData.CreateHandler(dbContext);
        await handler.HandleAsync(ContainerTestData.Command(typeId), _ct);

        var result = await handler.HandleAsync(ContainerTestData.Command(typeId), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Container.NumberExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.Containers.CountAsync(_ct));
    }
}
