using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Containers.GetAll;

public sealed class GetContainersHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainersQuery, Result<IReadOnlyList<ContainerResponse>>>
{
    public async Task<Result<IReadOnlyList<ContainerResponse>>> HandleAsync(
        GetContainersQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ContainerResponse> containers = await dbContext.Containers
            .AsNoTracking()
            .OrderBy(x => x.Number)
            .Select(x => new ContainerResponse(
                x.Id,
                x.Number,
                x.LineOperatorId,
                x.ContainerTypeId,
                x.IsoCode,
                x.SizeFeet,
                x.MaxWeightKg,
                x.TareWeightKg,
                x.ManufactureDate))
            .ToListAsync(cancellationToken);

        return Result.Success(containers);
    }
}
