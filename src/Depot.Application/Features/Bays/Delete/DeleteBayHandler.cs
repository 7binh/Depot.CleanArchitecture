using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Bays.Delete;

public sealed class DeleteBayHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteBayCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteBayCommand command,
        CancellationToken cancellationToken = default)
    {
        var bay = await dbContext.Bays
            .FirstOrDefaultAsync(x => x.Id == command.BayId && x.BlockId == command.BlockId, cancellationToken);
        if (bay is null)
        {
            return Result.Failure(BayErrors.BayNotFound(command.BayId));
        }

        var stands = await dbContext.AnyStandsInBayAsync(bay.Id, cancellationToken)
            || await dbContext.AnyStandsOverAsync(bay, null, cancellationToken);
        var removable = Bay.CanBeRemoved(stands);
        if (removable.IsFailure)
        {
            return removable;
        }

        return await RemoveAsync(bay, cancellationToken);
    }

    // The bay goes with its rows. A container that gated in after the check above makes the foreign key refuse it.
    private async Task<Result> RemoveAsync(Bay bay, CancellationToken cancellationToken)
    {
        var rows = await dbContext.YardRows.Where(x => x.BayId == bay.Id).ToListAsync(cancellationToken);

        var forgotten = await dbContext.ForgetRowsInClosedStaysAsync(rows.Select(x => x.Id).ToList(), cancellationToken);
        if (forgotten.IsFailure)
        {
            return forgotten;
        }

        dbContext.YardRows.RemoveRange(rows);
        dbContext.Bays.Remove(bay);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Bay.CanBeRemoved(true);
        }

        return Result.Success();
    }
}
