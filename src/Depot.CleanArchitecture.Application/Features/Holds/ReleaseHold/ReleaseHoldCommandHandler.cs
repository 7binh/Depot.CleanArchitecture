namespace Depot.CleanArchitecture.Application.Features.Holds.ReleaseHold;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class ReleaseHoldCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<ReleaseHoldCommand, Result>
{
    public async Task<Result> HandleAsync(
        ReleaseHoldCommand command,
        CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.ContainerVisits
            .Include(v => v.Holds)
            .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

        if (visit is null)
        {
            return Result.Failure(
                Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt lưu bãi có mã '{command.VisitId}'!"));
        }

        var releaseResult = visit.ReleaseHold(command.HoldId, command.ReleaseReason, command.ReleasedBy);
        if (releaseResult.IsFailure)
        {
            return releaseResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
