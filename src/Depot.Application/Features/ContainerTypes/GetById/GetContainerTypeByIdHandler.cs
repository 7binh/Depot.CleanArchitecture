using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerTypes.GetById;

public sealed class GetContainerTypeByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainerTypeByIdQuery, Result<ContainerTypeResponse>>
{
    public async Task<Result<ContainerTypeResponse>> HandleAsync(
        GetContainerTypeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await dbContext.ContainerTypes
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ContainerTypeResponse(x.Id, x.Code, x.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<ContainerTypeResponse>(
                Error.NotFound("ContainerType.NotFound", $"Container type '{query.Id}' was not found."));
        }

        return Result.Success(response);
    }
}
