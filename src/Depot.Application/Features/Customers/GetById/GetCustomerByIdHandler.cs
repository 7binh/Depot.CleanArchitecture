using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Customers.GetById;

public sealed class GetCustomerByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCustomerByIdQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new CustomerResponse(x.Id, x.TaxCode, x.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<CustomerResponse>(
                Error.NotFound("Customer.NotFound", $"Customer '{query.Id}' was not found."));
        }

        return Result.Success(response);
    }
}
