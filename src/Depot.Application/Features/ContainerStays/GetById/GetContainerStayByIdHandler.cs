using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays.GetById;

public sealed class GetContainerStayByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainerStayByIdQuery, Result<ContainerStayResponse>>
{
    public async Task<Result<ContainerStayResponse>> HandleAsync(
        GetContainerStayByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var stay = await dbContext.ContainerStays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (stay is null)
        {
            return Result.Failure<ContainerStayResponse>(ContainerStayErrors.StayNotFound(query.Id));
        }

        return Result.Success(ContainerStayResponse.From(stay));
    }
}
