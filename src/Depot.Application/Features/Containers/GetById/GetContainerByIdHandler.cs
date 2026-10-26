using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Containers.GetById;

public sealed class GetContainerByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainerByIdQuery, Result<ContainerResponse>>
{
    public async Task<Result<ContainerResponse>> HandleAsync(
        GetContainerByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Containers
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<ContainerResponse>(ContainerErrors.ContainerNotFound(query.Id));
        }

        return Result.Success(response);
    }
}
