namespace Depot.CleanArchitecture.Application.Features.Yard.GetYardMovements;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class GetYardMovementsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetYardMovementsQuery, Result<List<MovementHistoryItemDto>>>
{
    public async Task<Result<List<MovementHistoryItemDto>>> HandleAsync(
        GetYardMovementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var movementsQuery = dbContext.MovementEvents.AsNoTracking();

        if (query.VisitId.HasValue)
        {
            movementsQuery = movementsQuery.Where(m => m.VisitId == query.VisitId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(query.ContainerNumber))
        {
            var numRes = ContainerNumber.Create(query.ContainerNumber);
            if (numRes.IsSuccess)
            {
                var contNum = numRes.Value!;
                var cont = await dbContext.Containers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Number == contNum, cancellationToken);

                if (cont is not null)
                {
                    var visitIds = await dbContext.ContainerVisits.AsNoTracking()
                        .Where(v => v.ContainerId == cont.Id)
                        .Select(v => v.Id)
                        .ToListAsync(cancellationToken);

                    movementsQuery = movementsQuery.Where(m => visitIds.Contains(m.VisitId));
                }
            }
        }

        var movements = await movementsQuery
            .OrderByDescending(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var slotIds = movements
            .Select(m => m.FromSlotId)
            .Concat(movements.Select(m => m.ToSlotId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var slots = await dbContext.YardSlots.AsNoTracking()
            .Where(s => slotIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var blocks = await dbContext.YardBlocks.AsNoTracking()
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        int stt = 1;
        var dtos = movements.Select(m =>
        {
            string fromLoc = "Cổng vào (Gate In)";
            if (m.FromSlotId.HasValue && slots.TryGetValue(m.FromSlotId.Value, out var fs))
            {
                var bCode = blocks.TryGetValue(fs.BlockId, out var fb) ? fb.BlockCode : string.Empty;
                char rLetter = (char)('A' + fs.Row - 1);
                fromLoc = $"{bCode}-{rLetter}{fs.Bay:D2}-{fs.Tier:D2}";
            }

            string toLoc = "Bãi lưu trữ";
            if (m.ToSlotId.HasValue && slots.TryGetValue(m.ToSlotId.Value, out var ts))
            {
                var bCode = blocks.TryGetValue(ts.BlockId, out var tb) ? tb.BlockCode : string.Empty;
                char rLetter = (char)('A' + ts.Row - 1);
                toLoc = $"{bCode}-{rLetter}{ts.Bay:D2}-{ts.Tier:D2}";
            }

            string activity = m.EventType switch
            {
                "RELOCATE_YARD" => "Đảo chuyển bãi",
                "PLACE_YARD" => "Hạ bãi",
                "GATE_IN" => "Nhập bãi",
                "MOVE_TO_VIRTUAL" => "Chuyển block ảo",
                _ => m.EventType
            };

            string badgeClass = m.EventType switch
            {
                "GATE_IN" => "bg-emerald-50 text-emerald-600 border border-emerald-200",
                "RELOCATE_YARD" => "bg-blue-50 text-blue-600 border border-blue-200",
                _ => "bg-slate-50 text-slate-600 border border-slate-200"
            };

            return new MovementHistoryItemDto(
                stt++,
                m.OccurredAt.ToString("dd/MM/yyyy HH:mm"),
                fromLoc,
                toLoc,
                activity,
                badgeClass,
                m.ActorId,
                m.Reason ?? "Thực hiện theo lệnh điều độ"
            );
        }).ToList();

        return Result.Success(dtos);
    }
}
