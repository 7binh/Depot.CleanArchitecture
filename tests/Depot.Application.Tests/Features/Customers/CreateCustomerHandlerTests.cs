using Depot.Application.Features.Customers.Create;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Customers;

public sealed class CreateCustomerHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_SavesCustomer_WhenInputIsValid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(dbContext);

        var result = await handler.HandleAsync(new CreateCustomerCommand(" 0101234567 ", "Cong ty ABC"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("0101234567", result.Value.TaxCode);
        Assert.Equal("Cong ty ABC", result.Value.Name);
        Assert.Equal(1, await dbContext.Customers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenTaxCodeAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(dbContext);
        await handler.HandleAsync(new CreateCustomerCommand("0101234567", "Cong ty ABC"), _ct);

        var result = await handler.HandleAsync(new CreateCustomerCommand(" 0101234567 ", "Another Name"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Customer.TaxCodeExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(1, await dbContext.Customers.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationError_AndSavesNothing_WhenTaxCodeIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(dbContext);

        var result = await handler.HandleAsync(new CreateCustomerCommand("12345", "Cong ty ABC"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("TaxCode.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, await dbContext.Customers.CountAsync(_ct));
    }
}
