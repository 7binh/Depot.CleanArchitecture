using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Customers.GetAll;

public sealed class GetCustomersHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerResponse>>>
{
    public async Task<Result<IReadOnlyList<CustomerResponse>>> HandleAsync(
        GetCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomerResponse> customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.TaxCode)
            .Select(x => new CustomerResponse(x.Id, x.TaxCode, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(customers);
    }
}
