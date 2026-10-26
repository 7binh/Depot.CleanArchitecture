using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Bays.GetAll;

public sealed class GetBaysHandler(IAppDbContext dbContext)
    : IQueryHandler<GetBaysQuery, Result<IReadOnlyList<BayResponse>>>
{
    public async Task<Result<IReadOnlyList<BayResponse>>> HandleAsync(
        GetBaysQuery query,
        CancellationToken cancellationToken = default)
    {
        var blockExists = await dbContext.Blocks.AnyAsync(x => x.Id == query.BlockId, cancellationToken);
        if (!blockExists)
        {
            return Result.Failure<IReadOnlyList<BayResponse>>(BayErrors.BlockNotFound(query.BlockId));
        }

        IReadOnlyList<BayResponse> bays = await dbContext.Bays
            .AsNoTracking()
            .Where(x => x.BlockId == query.BlockId)
            .OrderBy(x => x.BayNo)
            .Select(x => new BayResponse(x.Id, x.BlockId, x.BayNo))
            .ToListAsync(cancellationToken);

        return Result.Success(bays);
    }
}
