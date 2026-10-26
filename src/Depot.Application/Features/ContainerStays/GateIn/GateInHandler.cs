using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Gate;
using Depot.Domain.Gate.Rules;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays.GateIn;

public sealed class GateInHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<GateInCommand, Result<ContainerStayResponse>>
{
    public async Task<Result<ContainerStayResponse>> HandleAsync(
        GateInCommand command,
        CancellationToken cancellationToken = default)
    {
        var container = await dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ContainerId, cancellationToken);
        if (container is null)
        {
            return Result.Failure<ContainerStayResponse>(ContainerStayErrors.ContainerNotFound(command.ContainerId));
        }

        var placement = await LoadPlacementAsync(command, cancellationToken);
        if (placement.IsFailure)
        {
            return Result.Failure<ContainerStayResponse>(placement.Error);
        }

        var hasOpenStay = await HasOpenStayAsync(container.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var details = new GateInDetails(
            command.GateInAt ?? now,
            command.Grade,
            command.Condition,
            command.DamageNote,
            new Vehicle(command.InVehicleType, command.InVehicleNumber));

        var created = ContainerStay.GateIn(container, hasOpenStay, details, placement.Value, now);
        if (created.IsFailure)
        {
            return Result.Failure<ContainerStayResponse>(created.Error);
        }

        return await SaveAsync(created.Value, cancellationToken);
    }

    // Loads the block, and for a row also its bay and the tiers already taken. A row that does not match the block
    // is left for the Domain rules to reject, so this method only reports what does not exist.
    private async Task<Result<GateInPlacement>> LoadPlacementAsync(
        GateInCommand command,
        CancellationToken cancellationToken)
    {
        var block = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<GateInPlacement>(ContainerStayErrors.BlockNotFound(command.BlockId));
        }

        if (command.RowId is null)
        {
            return Result.Success(new GateInPlacement(block, null, null, command.Tier, []));
        }

        var row = await dbContext.YardRows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.RowId, cancellationToken);
        if (row is null)
        {
            return Result.Failure<GateInPlacement>(ContainerStayErrors.RowNotFound(command.RowId.Value));
        }

        var bay = await dbContext.Bays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == row.BayId, cancellationToken);
        var occupiedTiers = await OccupiedTiersAsync(row.Id, cancellationToken);
        var neighbours = bay is null
            ? NeighbourTiers.None
            : await dbContext.LoadNeighbourTiersAsync(bay, row, cancellationToken);

        return Result.Success(new GateInPlacement(block, bay, row, command.Tier, occupiedTiers, neighbours));
    }

    // Only open stays count: a container that has left may come back, and its old position is free again.
    private Task<bool> HasOpenStayAsync(Guid containerId, CancellationToken cancellationToken)
    {
        return dbContext.ContainerStays
            .AnyAsync(x => x.ContainerId == containerId && x.GateOutAt == null, cancellationToken);
    }

    private async Task<IReadOnlyCollection<int>> OccupiedTiersAsync(Guid rowId, CancellationToken cancellationToken)
    {
        return await dbContext.ContainerStays
            .Where(x => x.RowId == rowId && x.Tier != null && x.GateOutAt == null)
            .Select(x => x.Tier!.Value)
            .ToListAsync(cancellationToken);
    }

    private async Task<Result<ContainerStayResponse>> SaveAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        dbContext.ContainerStays.Add(stay);
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

    // Two clerks can pass the checks at the same time; the unique indexes then reject the second save.
    // The failure is rebuilt from the same Domain rules so the message and code stay identical.
    private async Task<Result> FindConflictAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        var others = dbContext.ContainerStays.Where(x => x.Id != stay.Id && x.GateOutAt == null);
        var containerTaken = await others.AnyAsync(x => x.ContainerId == stay.ContainerId, cancellationToken);
        var slotTaken = stay.RowId is not null
            && await others.AnyAsync(x => x.RowId == stay.RowId && x.Tier == stay.Tier, cancellationToken);

        return BusinessRuleValidator.Check(
            new ContainerNotInYardRule(containerTaken),
            new SlotFreeRule(stay.Tier, slotTaken ? [stay.Tier!.Value] : []));
    }
}
