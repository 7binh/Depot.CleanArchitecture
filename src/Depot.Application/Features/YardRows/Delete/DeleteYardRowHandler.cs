using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.YardRows.Delete;

public sealed class DeleteYardRowHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteYardRowCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteYardRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var bay = await dbContext.Bays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.BayId && x.BlockId == command.BlockId, cancellationToken);
        if (bay is null)
        {
            return Result.Failure(BayErrors.BayNotFound(command.BayId));
        }

        var row = await dbContext.YardRows
            .FirstOrDefaultAsync(x => x.Id == command.RowId && x.BayId == command.BayId, cancellationToken);
        if (row is null)
        {
            return Result.Failure(YardRowErrors.RowNotFound(command.RowId));
        }

        var stands = await dbContext.AnyStandsInRowAsync(row.Id, cancellationToken)
            || await dbContext.AnyStandsOverAsync(bay, row.RowNo, cancellationToken);
        var removable = YardRow.CanBeRemoved(stands);
        if (removable.IsFailure)
        {
            return removable;
        }

        return await RemoveAsync(row, cancellationToken);
    }

    // A container that gated in after the check above makes the foreign key refuse the delete, so nothing is lost.
    private async Task<Result> RemoveAsync(YardRow row, CancellationToken cancellationToken)
    {
        var forgotten = await dbContext.ForgetRowsInClosedStaysAsync([row.Id], cancellationToken);
        if (forgotten.IsFailure)
        {
            return forgotten;
        }

        dbContext.YardRows.Remove(row);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return YardRow.CanBeRemoved(true);
        }

        return Result.Success();
    }
}
