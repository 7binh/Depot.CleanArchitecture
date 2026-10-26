using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.Update;

public sealed class UpdateBlockHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateBlockCommand, Result<BlockResponse>>
{
    public async Task<Result<BlockResponse>> HandleAsync(
        UpdateBlockCommand command,
        CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (block is null)
        {
            return Result.Failure<BlockResponse>(
                Error.NotFound("Block.NotFound", $"Block '{command.Id}' was not found."));
        }

        var renamed = block.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return Result.Failure<BlockResponse>(renamed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new BlockResponse(block.Id, block.Code, block.Name, block.IsVirtual));
    }
}
