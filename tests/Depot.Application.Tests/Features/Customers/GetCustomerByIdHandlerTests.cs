using Depot.Application.Features.Customers.Create;
using Depot.Application.Features.Customers.GetById;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Customers;

public sealed class GetCustomerByIdHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsCustomer_WhenIdExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var created = await new CreateCustomerHandler(dbContext)
            .HandleAsync(new CreateCustomerCommand("0101234567", "Cong ty ABC"), _ct);
        var handler = new GetCustomerByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetCustomerByIdQuery(created.Value.Id), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value.Id);
        Assert.Equal("0101234567", result.Value.TaxCode);
        Assert.Equal("Cong ty ABC", result.Value.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetCustomerByIdHandler(dbContext);

        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.NewGuid()), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Customer.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
