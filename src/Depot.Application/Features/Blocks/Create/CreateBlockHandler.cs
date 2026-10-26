using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.Create;

public sealed class CreateBlockHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateBlockCommand, Result<BlockResponse>>
{
    public async Task<Result<BlockResponse>> HandleAsync(
        CreateBlockCommand command,
        CancellationToken cancellationToken = default)
    {
        var created = Block.Create(command.Code, command.Name, command.IsVirtual);
        if (created.IsFailure)
        {
            return Result.Failure<BlockResponse>(created.Error);
        }

        var block = created.Value;

        if (await CodeExistsAsync(block.Code, cancellationToken))
        {
            return CodeExistsFailure(block.Code);
        }

        dbContext.Blocks.Add(block);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await CodeExistsAsync(block.Code, cancellationToken))
            {
                throw;
            }

            return CodeExistsFailure(block.Code);
        }

        return Result.Success(new BlockResponse(block.Id, block.Code, block.Name, block.IsVirtual));
    }

    private Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.Blocks.AnyAsync(x => x.Code == code, cancellationToken);
    }

    private static Result<BlockResponse> CodeExistsFailure(string code)
    {
        return Result.Failure<BlockResponse>(
            Error.Conflict("Block.CodeExists", $"Block code '{code}' already exists."));
    }
}
