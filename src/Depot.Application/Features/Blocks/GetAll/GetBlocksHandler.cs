using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.GetAll;

public sealed class GetBlocksHandler(IAppDbContext dbContext)
    : IQueryHandler<GetBlocksQuery, Result<IReadOnlyList<BlockResponse>>>
{
    public async Task<Result<IReadOnlyList<BlockResponse>>> HandleAsync(
        GetBlocksQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BlockResponse> blocks = await dbContext.Blocks
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new BlockResponse(x.Id, x.Code, x.Name, x.IsVirtual))
            .ToListAsync(cancellationToken);

        return Result.Success(blocks);
    }
}
