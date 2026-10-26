using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.Delete;

public sealed class DeleteBlockHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteBlockCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteBlockCommand command,
        CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (block is null)
        {
            return Result.Failure(Error.NotFound("Block.NotFound", $"Block '{command.Id}' was not found."));
        }

        // Every stay records its block, so a block that ever held a container has history to keep.
        var hasStays = await dbContext.ContainerStays.AnyAsync(x => x.BlockId == block.Id, cancellationToken);
        var removable = Block.CanBeRemoved(hasStays);
        if (removable.IsFailure)
        {
            return removable;
        }

        return await RemoveAsync(block, cancellationToken);
    }

    // The block goes with its bays and rows. A container that gated in after the check above makes the foreign key
    // refuse it.
    private async Task<Result> RemoveAsync(Block block, CancellationToken cancellationToken)
    {
        var bays = await dbContext.Bays.Where(x => x.BlockId == block.Id).ToListAsync(cancellationToken);
        var bayIds = bays.Select(x => x.Id).ToList();
        var rows = await dbContext.YardRows.Where(x => bayIds.Contains(x.BayId)).ToListAsync(cancellationToken);

        dbContext.YardRows.RemoveRange(rows);
        dbContext.Bays.RemoveRange(bays);
        dbContext.Blocks.Remove(block);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Block.CanBeRemoved(true);
        }

        return Result.Success();
    }
}
