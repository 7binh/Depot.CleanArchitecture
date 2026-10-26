using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;
using Depot.Domain.Yard.Rules;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.GenerateLayout;

public sealed class GenerateLayoutHandler(IAppDbContext dbContext)
    : ICommandHandler<GenerateLayoutCommand, Result<GenerateLayoutResponse>>
{
    public async Task<Result<GenerateLayoutResponse>> HandleAsync(
        GenerateLayoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<GenerateLayoutResponse>(BayErrors.BlockNotFound(command.BlockId));
        }

        var check = BusinessRuleValidator.Check(
            new BlockHasBaysRule(block.IsVirtual),
            new BayRangeRule(command.FromBay, command.ToBay),
            new RowCountRule(command.RowCount),
            new MaxTierRule(command.MaxTier));
        if (check.IsFailure)
        {
            return Result.Failure<GenerateLayoutResponse>(check.Error);
        }

        var existing = await LoadExistingAsync(command, cancellationToken);
        var added = Build(block, command, existing);

        dbContext.Bays.AddRange(added.Bays);
        dbContext.YardRows.AddRange(added.Rows);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await ChangedMeanwhileAsync(command, existing, cancellationToken))
            {
                throw;
            }

            return Result.Failure<GenerateLayoutResponse>(BayErrors.LayoutChanged());
        }

        return Result.Success(new GenerateLayoutResponse(added.Bays.Count, added.Rows.Count));
    }

    private sealed record Existing(Dictionary<int, Bay> BaysByNo, HashSet<(Guid BayId, int RowNo)> Rows);

    private sealed record Added(List<Bay> Bays, List<YardRow> Rows);

    // Only the bays of the range count, and only their rows; the rest of the block is not touched.
    private async Task<Existing> LoadExistingAsync(GenerateLayoutCommand command, CancellationToken cancellationToken)
    {
        var bays = await dbContext.Bays
            .AsNoTracking()
            .Where(x => x.BlockId == command.BlockId && x.BayNo >= command.FromBay && x.BayNo <= command.ToBay)
            .ToListAsync(cancellationToken);
        var bayIds = bays.Select(x => x.Id).ToList();
        var rows = await dbContext.YardRows
            .AsNoTracking()
            .Where(x => bayIds.Contains(x.BayId))
            .Select(x => new { x.BayId, x.RowNo })
            .ToListAsync(cancellationToken);

        return new Existing(
            bays.ToDictionary(x => x.BayNo),
            rows.Select(x => (x.BayId, x.RowNo)).ToHashSet());
    }

    // The checks above already passed, so the Domain factories cannot fail here.
    private static Added Build(Block block, GenerateLayoutCommand command, Existing existing)
    {
        var bays = new List<Bay>();
        var rows = new List<YardRow>();

        for (var bayNo = command.FromBay; bayNo <= command.ToBay; bayNo++)
        {
            if (!existing.BaysByNo.TryGetValue(bayNo, out var bay))
            {
                bay = Bay.Create(block, bayNo).Value;
                bays.Add(bay);
            }

            for (var rowNo = 1; rowNo <= command.RowCount; rowNo++)
            {
                if (!existing.Rows.Contains((bay.Id, rowNo)))
                {
                    rows.Add(YardRow.Create(bay.Id, rowNo, command.MaxTier).Value);
                }
            }
        }

        return new Added(bays, rows);
    }

    // Another request may have created some of the same bays or rows between our read and our save.
    private async Task<bool> ChangedMeanwhileAsync(
        GenerateLayoutCommand command,
        Existing before,
        CancellationToken cancellationToken)
    {
        var now = await LoadExistingAsync(command, cancellationToken);
        return now.BaysByNo.Count != before.BaysByNo.Count || now.Rows.Count != before.Rows.Count;
    }
}
