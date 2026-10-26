using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerTypes.GetAll;

public sealed class GetContainerTypesHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainerTypesQuery, Result<IReadOnlyList<ContainerTypeResponse>>>
{
    public async Task<Result<IReadOnlyList<ContainerTypeResponse>>> HandleAsync(
        GetContainerTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ContainerTypeResponse> containerTypes = await dbContext.ContainerTypes
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ContainerTypeResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(containerTypes);
    }
}
