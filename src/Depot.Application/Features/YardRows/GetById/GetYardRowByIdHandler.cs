using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.YardRows.GetById;

public sealed class GetYardRowByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetYardRowByIdQuery, Result<YardRowResponse>>
{
    public async Task<Result<YardRowResponse>> HandleAsync(
        GetYardRowByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var bayExists = await dbContext.Bays
            .AnyAsync(x => x.Id == query.BayId && x.BlockId == query.BlockId, cancellationToken);
        if (!bayExists)
        {
            return Result.Failure<YardRowResponse>(BayErrors.BayNotFound(query.BayId));
        }

        var response = await dbContext.YardRows
            .AsNoTracking()
            .Where(x => x.Id == query.RowId && x.BayId == query.BayId)
            .Select(x => new YardRowResponse(x.Id, x.BayId, x.RowNo, x.MaxTier))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<YardRowResponse>(YardRowErrors.RowNotFound(query.RowId));
        }

        return Result.Success(response);
    }
}
