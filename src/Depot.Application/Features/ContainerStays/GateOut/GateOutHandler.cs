using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depot.Application.Features.ContainerStays.GateOut;

public sealed class GateOutHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<GateOutCommand, Result<ContainerStayResponse>>
{
    public async Task<Result<ContainerStayResponse>> HandleAsync(
        GateOutCommand command,
        CancellationToken cancellationToken = default)
    {
        var container = await dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ContainerId, cancellationToken);
        if (container is null)
        {
            return Result.Failure<ContainerStayResponse>(
                ContainerStayErrors.GateOutContainerNotFound(command.ContainerId));
        }

        var order = await dbContext.ReleaseOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.ReleaseOrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<ContainerStayResponse>(
                ContainerStayErrors.GateOutReleaseOrderNotFound(command.ReleaseOrderId));
        }

        // Gate outs on one order run one at a time, so the delivered count below cannot go stale (BR-RO-06).
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
        await dbContext.LockReleaseOrderAsync(order.Id, cancellationToken);

        var stay = await dbContext.ContainerStays
            .FirstOrDefaultAsync(x => x.ContainerId == container.Id && x.GateOutAt == null, cancellationToken);
        if (stay is null)
        {
            return Result.Failure<ContainerStayResponse>(ContainerStayErrors.NotInYard(container.Id));
        }

        var delivered = await DeliveredOnLineAsync(order.FindItemFor(container), cancellationToken);
        var now = timeProvider.GetUtcNow();
        var details = new GateOutDetails(
            command.GateOutAt ?? now,
            new Vehicle(command.OutVehicleType, command.OutVehicleNumber));

        var hasContainerAbove = await HasContainerAboveAsync(stay, cancellationToken);
        var done = stay.GateOut(
            container,
            new GateOutRelease(order, delivered),
            details,
            now,
            DepotDateOf(details.GateOutAt),
            hasContainerAbove);
        if (done.IsFailure)
        {
            return Result.Failure<ContainerStayResponse>(done.Error);
        }

        return await SaveAsync(stay, transaction, cancellationToken);
    }

    // The same container may leave on another order at the same moment: whoever saves second loses.
    private async Task<Result<ContainerStayResponse>> SaveAsync(
        ContainerStay stay,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<ContainerStayResponse>(ContainerStayErrors.NotInYard(stay.ContainerId));
        }

        return Result.Success(ContainerStayResponse.From(stay));
    }

    // BR-YD-06: a container on a higher tier of the same row is still resting on this one. A virtual block has no tiers.
    private async Task<bool> HasContainerAboveAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        if (stay.RowId is null || stay.Tier is null)
        {
            return false;
        }

        var stackedInRow = await dbContext.ContainerStays
            .AnyAsync(x => x.RowId == stay.RowId && x.GateOutAt == null && x.Tier > stay.Tier, cancellationToken);

        return stackedInRow || await IsCarryingFortyFootAsync(stay, cancellationToken);
    }

    // A 20ft container in an odd bay can carry a 40ft one whose own row is in the even bay beside it.
    private async Task<bool> IsCarryingFortyFootAsync(ContainerStay stay, CancellationToken cancellationToken)
    {
        var row = await dbContext.YardRows.AsNoTracking().FirstAsync(x => x.Id == stay.RowId, cancellationToken);
        var bay = await dbContext.Bays.AsNoTracking().FirstAsync(x => x.Id == row.BayId, cancellationToken);
        if (bay.BayNo % 2 == 0)
        {
            return false;
        }

        var neighbours = await dbContext.LoadNeighbourTiersAsync(bay, row, cancellationToken);
        return neighbours.Before.Concat(neighbours.After).Any(tier => tier > stay.Tier);
    }

    // BR-RO-06: how many containers already left on this line, across all stays.
    private async Task<int> DeliveredOnLineAsync(ReleaseOrderItem? item, CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return 0;
        }

        return await dbContext.ContainerStays.CountAsync(x => x.ReleaseOrderItemId == item.Id, cancellationToken);
    }

    // The expiry date is a calendar day at the depot, so convert before taking the date (BR-RO-04).
    private DateOnly DepotDateOf(DateTimeOffset gateOutAt)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(gateOutAt, timeProvider.LocalTimeZone).DateTime);
    }
}
