using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.YardRows.GetAll;

public sealed class GetYardRowsHandler(IAppDbContext dbContext)
    : IQueryHandler<GetYardRowsQuery, Result<IReadOnlyList<YardRowResponse>>>
{
    public async Task<Result<IReadOnlyList<YardRowResponse>>> HandleAsync(
        GetYardRowsQuery query,
        CancellationToken cancellationToken = default)
    {
        var bayExists = await dbContext.Bays
            .AnyAsync(x => x.Id == query.BayId && x.BlockId == query.BlockId, cancellationToken);
        if (!bayExists)
        {
            return Result.Failure<IReadOnlyList<YardRowResponse>>(BayErrors.BayNotFound(query.BayId));
        }

        IReadOnlyList<YardRowResponse> rows = await dbContext.YardRows
            .AsNoTracking()
            .Where(x => x.BayId == query.BayId)
            .OrderBy(x => x.RowNo)
            .Select(x => new YardRowResponse(x.Id, x.BayId, x.RowNo, x.MaxTier))
            .ToListAsync(cancellationToken);

        return Result.Success(rows);
    }
}
