namespace Depot.CleanArchitecture.Application.Features.Holds.ApplyHold;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class ApplyHoldCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<ApplyHoldCommand, Result<ApplyHoldResponse>>
{
    public async Task<Result<ApplyHoldResponse>> HandleAsync(
        ApplyHoldCommand command,
        CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.ContainerVisits
            .Include(v => v.Holds)
            .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

        if (visit is null)
        {
            return Result.Failure<ApplyHoldResponse>(
                Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt lưu bãi có mã '{command.VisitId}'!"));
        }

        var holdResult = visit.ApplyHold(command.HoldCode, command.Reason, command.HeldBy);
        if (holdResult.IsFailure)
        {
            return Result.Failure<ApplyHoldResponse>(holdResult.Error);
        }

        var hold = holdResult.Value!;
        dbContext.StayHolds.Add(hold);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new ApplyHoldResponse(hold.Id, visit.Id, hold.HoldCode, hold.HeldAt));
    }
}
