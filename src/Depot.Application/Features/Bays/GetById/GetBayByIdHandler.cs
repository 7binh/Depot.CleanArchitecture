using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Bays.GetById;

public sealed class GetBayByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetBayByIdQuery, Result<BayResponse>>
{
    public async Task<Result<BayResponse>> HandleAsync(
        GetBayByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Bays
            .AsNoTracking()
            .Where(x => x.Id == query.BayId && x.BlockId == query.BlockId)
            .Select(x => new BayResponse(x.Id, x.BlockId, x.BayNo))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<BayResponse>(BayErrors.BayNotFound(query.BayId));
        }

        return Result.Success(response);
    }
}
