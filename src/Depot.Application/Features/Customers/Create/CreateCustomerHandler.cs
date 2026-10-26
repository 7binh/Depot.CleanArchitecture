using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Customers.Create;

public sealed class CreateCustomerHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var created = Customer.Create(command.TaxCode, command.Name);
        if (created.IsFailure)
        {
            return Result.Failure<CustomerResponse>(created.Error);
        }

        var customer = created.Value;

        if (await TaxCodeExistsAsync(customer.TaxCode, cancellationToken))
        {
            return TaxCodeExistsFailure(customer.TaxCode);
        }

        dbContext.Customers.Add(customer);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await TaxCodeExistsAsync(customer.TaxCode, cancellationToken))
            {
                throw;
            }

            return TaxCodeExistsFailure(customer.TaxCode);
        }

        return Result.Success(new CustomerResponse(customer.Id, customer.TaxCode, customer.Name));
    }

    private Task<bool> TaxCodeExistsAsync(string taxCode, CancellationToken cancellationToken)
    {
        return dbContext.Customers.AnyAsync(x => x.TaxCode == taxCode, cancellationToken);
    }

    private static Result<CustomerResponse> TaxCodeExistsFailure(string taxCode)
    {
        return Result.Failure<CustomerResponse>(
            Error.Conflict("Customer.TaxCodeExists", $"Customer tax code '{taxCode}' already exists."));
    }
}
