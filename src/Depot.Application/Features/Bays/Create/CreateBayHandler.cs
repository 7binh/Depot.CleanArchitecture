using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Bays.Create;

public sealed class CreateBayHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateBayCommand, Result<BayResponse>>
{
    public async Task<Result<BayResponse>> HandleAsync(
        CreateBayCommand command,
        CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<BayResponse>(BayErrors.BlockNotFound(command.BlockId));
        }

        var created = Bay.Create(block, command.BayNo);
        if (created.IsFailure)
        {
            return Result.Failure<BayResponse>(created.Error);
        }

        var bay = created.Value;

        if (await BayNoExistsAsync(bay, cancellationToken))
        {
            return Result.Failure<BayResponse>(BayErrors.BayNoExists(bay.BayNo));
        }

        dbContext.Bays.Add(bay);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await BayNoExistsAsync(bay, cancellationToken))
            {
                throw;
            }

            return Result.Failure<BayResponse>(BayErrors.BayNoExists(bay.BayNo));
        }

        return Result.Success(new BayResponse(bay.Id, bay.BlockId, bay.BayNo));
    }

    private Task<bool> BayNoExistsAsync(Bay bay, CancellationToken cancellationToken)
    {
        return dbContext.Bays.AnyAsync(x => x.BlockId == bay.BlockId && x.BayNo == bay.BayNo, cancellationToken);
    }
}
