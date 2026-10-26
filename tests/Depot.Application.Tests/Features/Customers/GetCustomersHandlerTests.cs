using Depot.Application.Features.Customers.Create;
using Depot.Application.Features.Customers.GetAll;

namespace Depot.Application.Tests.Features.Customers;

public sealed class GetCustomersHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoCustomerExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetCustomersHandler(dbContext);

        var result = await handler.HandleAsync(new GetCustomersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCustomersOrderedByTaxCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var create = new CreateCustomerHandler(dbContext);
        await create.HandleAsync(new CreateCustomerCommand("0300000003", "C"), _ct);
        await create.HandleAsync(new CreateCustomerCommand("0100000001", "A"), _ct);
        await create.HandleAsync(new CreateCustomerCommand("0200000002001", "B"), _ct);
        var handler = new GetCustomersHandler(dbContext);

        var result = await handler.HandleAsync(new GetCustomersQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(["0100000001", "0200000002001", "0300000003"], result.Value.Select(x => x.TaxCode));
    }
}
