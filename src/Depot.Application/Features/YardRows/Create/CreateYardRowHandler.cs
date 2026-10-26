using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.YardRows.Create;

public sealed class CreateYardRowHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateYardRowCommand, Result<YardRowResponse>>
{
    public async Task<Result<YardRowResponse>> HandleAsync(
        CreateYardRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var bayExists = await dbContext.Bays
            .AnyAsync(x => x.Id == command.BayId && x.BlockId == command.BlockId, cancellationToken);
        if (!bayExists)
        {
            return Result.Failure<YardRowResponse>(BayErrors.BayNotFound(command.BayId));
        }

        var created = YardRow.Create(command.BayId, command.RowNo, command.MaxTier);
        if (created.IsFailure)
        {
            return Result.Failure<YardRowResponse>(created.Error);
        }

        var row = created.Value;

        if (await RowNoExistsAsync(row, cancellationToken))
        {
            return Result.Failure<YardRowResponse>(YardRowErrors.RowNoExists(row.RowNo));
        }

        dbContext.YardRows.Add(row);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await RowNoExistsAsync(row, cancellationToken))
            {
                throw;
            }

            return Result.Failure<YardRowResponse>(YardRowErrors.RowNoExists(row.RowNo));
        }

        return Result.Success(new YardRowResponse(row.Id, row.BayId, row.RowNo, row.MaxTier));
    }

    private Task<bool> RowNoExistsAsync(YardRow row, CancellationToken cancellationToken)
    {
        return dbContext.YardRows.AnyAsync(x => x.BayId == row.BayId && x.RowNo == row.RowNo, cancellationToken);
    }
}
