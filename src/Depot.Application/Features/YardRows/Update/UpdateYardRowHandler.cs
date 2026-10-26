using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.YardRows.Update;

public sealed class UpdateYardRowHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateYardRowCommand, Result<YardRowResponse>>
{
    public async Task<Result<YardRowResponse>> HandleAsync(
        UpdateYardRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var bayExists = await dbContext.Bays
            .AnyAsync(x => x.Id == command.BayId && x.BlockId == command.BlockId, cancellationToken);
        if (!bayExists)
        {
            return Result.Failure<YardRowResponse>(BayErrors.BayNotFound(command.BayId));
        }

        var row = await dbContext.YardRows
            .FirstOrDefaultAsync(x => x.Id == command.RowId && x.BayId == command.BayId, cancellationToken);
        if (row is null)
        {
            return Result.Failure<YardRowResponse>(YardRowErrors.RowNotFound(command.RowId));
        }

        var highestOccupiedTier = await HighestOccupiedTierAsync(row.Id, cancellationToken);
        var changed = row.ChangeMaxTier(command.MaxTier, highestOccupiedTier);
        if (changed.IsFailure)
        {
            return Result.Failure<YardRowResponse>(changed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new YardRowResponse(row.Id, row.BayId, row.RowNo, row.MaxTier));
    }

    // Only containers still in the yard count; one that has left no longer needs the tier (BR-YD-09).
    private async Task<int> HighestOccupiedTierAsync(Guid rowId, CancellationToken cancellationToken)
    {
        var highest = await dbContext.ContainerStays
            .Where(x => x.RowId == rowId && x.GateOutAt == null)
            .MaxAsync(x => x.Tier, cancellationToken);

        return highest ?? 0;
    }
}
