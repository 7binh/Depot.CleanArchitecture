using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.GetById;

public sealed class GetBlockByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetBlockByIdQuery, Result<BlockResponse>>
{
    public async Task<Result<BlockResponse>> HandleAsync(
        GetBlockByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Blocks
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new BlockResponse(x.Id, x.Code, x.Name, x.IsVirtual))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<BlockResponse>(
                Error.NotFound("Block.NotFound", $"Block '{query.Id}' was not found."));
        }

        return Result.Success(response);
    }
}
