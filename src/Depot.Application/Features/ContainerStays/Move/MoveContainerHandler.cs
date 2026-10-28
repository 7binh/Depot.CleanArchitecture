using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Gate;
using Depot.Domain.Gate.Rules;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays.Move;

public sealed class MoveContainerHandler(IAppDbContext dbContext)
    : ICommandHandler<MoveContainerCommand, Result<ContainerStayResponse>>
{
    public async Task<Result<ContainerStayResponse>> HandleAsync(
        MoveContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var stay = await dbContext.ContainerStays
            .FirstOrDefaultAsync(x => x.Id == command.StayId, cancellationToken);
        if (stay is null)
        {
            return Result.Failure<ContainerStayResponse>(ContainerStayErrors.StayNotFound(command.StayId));
        }

        var container = await dbContext.Containers
            .AsNoTracking()
            .FirstAsync(x => x.Id == stay.ContainerId, cancellationToken);

        var placement = await dbContext.LoadPlacementAsync(
            command.BlockId,
            command.RowId,
            command.Tier,
            stay.Id,
            cancellationToken);
        if (placement.IsFailure)
        {
            return Result.Failure<ContainerStayResponse>(placement.Error);
        }

        var hasContainerAbove = await dbContext.HasContainerAboveAsync(stay, cancellationToken);
        var moved = stay.MoveTo(container, placement.Value, hasContainerAbove);
        if (moved.IsFailure)
        {
            return Result.Failure<ContainerStayResponse>(moved.Error);
        }

        return await SaveAsync(stay, cancellationToken);
    }

    private async Task<Result<ContainerStayResponse>> SaveAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var conflict = await FindConflictAsync(stay, cancellationToken);
            if (conflict.IsSuccess)
            {
                throw;
            }

            return Result.Failure<ContainerStayResponse>(conflict.Error);
        }

        return Result.Success(ContainerStayResponse.From(stay));
    }

    // Two clerks can pass the checks at the same time; the unique index then rejects the second save.
    private async Task<Result> FindConflictAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        var slotTaken = stay.RowId is not null
            && await dbContext.ContainerStays.AnyAsync(
                x => x.Id != stay.Id && x.GateOutAt == null && x.RowId == stay.RowId && x.Tier == stay.Tier,
                cancellationToken);

        return BusinessRuleValidator.Check(new SlotFreeRule(stay.Tier, slotTaken ? [stay.Tier!.Value] : []));
    }
}
