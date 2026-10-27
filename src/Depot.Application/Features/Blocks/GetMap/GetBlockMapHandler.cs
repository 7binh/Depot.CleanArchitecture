using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Gate;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Blocks.GetMap;

public sealed class GetBlockMapHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetBlockMapQuery, Result<BlockMapResponse>>
{
    public async Task<Result<BlockMapResponse>> HandleAsync(
        GetBlockMapQuery query,
        CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<BlockMapResponse>(
                Error.NotFound("Block.NotFound", $"Block '{query.BlockId}' was not found."));
        }

        var bays = await dbContext.Bays
            .AsNoTracking()
            .Where(x => x.BlockId == block.Id)
            .OrderBy(x => x.BayNo)
            .ToListAsync(cancellationToken);
        var rows = await LoadRowsAsync(block.Id, cancellationToken);
        var stays = await LoadStaysAsync(block.Id, cancellationToken);

        var zone = timeProvider.LocalTimeZone;
        var today = DepotCalendar.DateOf(timeProvider.GetUtcNow(), zone);
        var containers = stays.ToDictionary(x => x.StayId, x => ToContainer(x, zone, today));

        var rowsByBay = rows.ToLookup(x => x.BayId);
        var staysByRow = stays.Where(x => x.RowId is not null).ToLookup(x => x.RowId!.Value);
        var bayMaps = bays
            .Select(bay => MapBay(bay, rowsByBay[bay.Id], staysByRow, containers))
            .ToList();
        var unplaced = stays
            .Where(x => x.RowId is null)
            .Select(x => containers[x.StayId])
            .OrderBy(x => x.Number, StringComparer.Ordinal)
            .ToList();

        return Result.Success(new BlockMapResponse(block.Id, block.Code, block.Name, block.IsVirtual, bayMaps, unplaced));
    }

    private static BayMap MapBay(
        Bay bay,
        IEnumerable<YardRow> rows,
        ILookup<Guid, StayInBlock> staysByRow,
        Dictionary<Guid, MapContainer> containers)
    {
        var (first, last) = bay.FloorSpan;
        var rowMaps = rows
            .OrderBy(x => x.RowNo)
            .Select(row => new RowMap(
                row.RowNo,
                row.MaxTier,
                staysByRow[row.Id]
                    .OrderBy(x => x.Tier)
                    .Select(x => new TierMap(x.Tier!.Value, first, last, containers[x.StayId]))
                    .ToList()))
            .ToList();

        return new BayMap(bay.BayNo, bay.SizeFeet, rowMaps);
    }

    private static MapContainer ToContainer(StayInBlock stay, TimeZoneInfo zone, DateOnly today)
    {
        var days = StayDuration.DaysBetween(DepotCalendar.DateOf(stay.GateInAt, zone), today);
        return new MapContainer(
            stay.StayId,
            stay.Number,
            stay.SizeFeet,
            stay.OperatorCode,
            days,
            StayDuration.IsLongStay(days),
            stay.IsDamaged);
    }

    private Task<List<YardRow>> LoadRowsAsync(Guid blockId, CancellationToken cancellationToken)
    {
        return dbContext.YardRows
            .AsNoTracking()
            .Join(dbContext.Bays, row => row.BayId, bay => bay.Id, (row, bay) => new { Row = row, bay.BlockId })
            .Where(x => x.BlockId == blockId)
            .Select(x => x.Row)
            .ToListAsync(cancellationToken);
    }

    // The containers now standing in the block, with the few container and operator fields the map shows.
    private Task<List<StayInBlock>> LoadStaysAsync(Guid blockId, CancellationToken cancellationToken)
    {
        return dbContext.ContainerStays
            .AsNoTracking()
            .Where(x => x.BlockId == blockId && x.GateOutAt == null)
            .Join(dbContext.Containers, stay => stay.ContainerId, container => container.Id, (stay, container) => new { stay, container })
            .Join(
                dbContext.LineOperators,
                x => x.container.LineOperatorId,
                lineOperator => lineOperator.Id,
                (x, lineOperator) => new StayInBlock(
                    x.stay.Id,
                    x.stay.RowId,
                    x.stay.Tier,
                    x.stay.GateInAt,
                    x.stay.Condition == StayCondition.Damaged,
                    x.container.Number,
                    x.container.SizeFeet,
                    lineOperator.Code))
            .ToListAsync(cancellationToken);
    }

    private sealed record StayInBlock(
        Guid StayId,
        Guid? RowId,
        int? Tier,
        DateTimeOffset GateInAt,
        bool IsDamaged,
        string Number,
        int SizeFeet,
        string OperatorCode);
}
