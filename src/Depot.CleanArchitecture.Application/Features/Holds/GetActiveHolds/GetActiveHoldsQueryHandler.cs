namespace Depot.CleanArchitecture.Application.Features.Holds.GetActiveHolds;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class GetActiveHoldsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetActiveHoldsQuery, Result<IReadOnlyList<StayHoldDto>>>
{
    public async Task<Result<IReadOnlyList<StayHoldDto>>> HandleAsync(
        GetActiveHoldsQuery query,
        CancellationToken cancellationToken = default)
    {
        var holdsQuery = dbContext.StayHolds.Where(h => h.ReleasedAt == null);

        if (query.VisitId.HasValue)
        {
            holdsQuery = holdsQuery.Where(h => h.VisitId == query.VisitId.Value);
        }

        var holds = await holdsQuery
            .OrderByDescending(h => h.HeldAt)
            .Select(h => new StayHoldDto(
                h.Id,
                h.VisitId,
                h.HoldCode,
                h.Reason,
                h.HeldAt,
                h.HeldBy,
                h.ReleasedAt,
                h.ReleasedBy,
                h.ReleaseReason,
                h.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StayHoldDto>>(holds);
    }
}
