namespace Depot.CleanArchitecture.Application.Features.Yard.GetYardLayout;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class GetYardLayoutQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetYardLayoutQuery, Result<YardLayoutResponse>>
{
    public async Task<Result<YardLayoutResponse>> HandleAsync(
        GetYardLayoutQuery query,
        CancellationToken cancellationToken = default)
    {
        var blocksQuery = dbContext.YardBlocks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.BlockCode))
        {
            blocksQuery = blocksQuery.Where(b => b.BlockCode == query.BlockCode);
        }

        var blocks = await blocksQuery.ToListAsync(cancellationToken);
        var blockIds = blocks.Select(b => b.Id).ToList();

        var slots = await dbContext.YardSlots
            .AsNoTracking()
            .Where(s => blockIds.Contains(s.BlockId))
            .ToListAsync(cancellationToken);

        var slotContainerIds = slots
            .Where(s => s.CurrentContainerId.HasValue)
            .Select(s => s.CurrentContainerId!.Value)
            .Distinct()
            .ToList();

        var activeVisits = await dbContext.ContainerVisits
            .AsNoTracking()
            .Include(v => v.Holds)
            .Include(v => v.Movements)
            .Where(v => v.Status != Domain.Enums.ContainerVisitStatus.GatedOut &&
                        v.Status != Domain.Enums.ContainerVisitStatus.Completed &&
                        ((v.CurrentBlockId.HasValue && blockIds.Contains(v.CurrentBlockId.Value) && v.CurrentSlotId == null) ||
                         slotContainerIds.Contains(v.ContainerId)))
            .ToListAsync(cancellationToken);

        var activeContainerIds = activeVisits.Select(v => v.ContainerId).ToHashSet();

        var virtualVisits = activeVisits
            .Where(v => v.CurrentBlockId.HasValue && blockIds.Contains(v.CurrentBlockId.Value) && v.CurrentSlotId == null)
            .ToList();

        var containerIds = slotContainerIds
            .Union(virtualVisits.Select(v => v.ContainerId))
            .Distinct()
            .ToList();

        var containers = await dbContext.Containers
            .AsNoTracking()
            .Where(c => containerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var visits = activeVisits
            .GroupBy(v => v.ContainerId)
            .ToDictionary(g => g.Key, g => g.First());

        var blockDict = blocks.ToDictionary(b => b.Id);

        var blockDtos = blocks.Select(b =>
        {
            if (b.IsVirtual)
            {
                var visitsInBlock = virtualVisits.Where(v => v.CurrentBlockId == b.Id).ToList();
                int totalContainers = visitsInBlock.Select(v => v.ContainerId).Distinct().Count();
                var tierStats = Array.Empty<BlockTierStatDto>();

                var virtualContainers = visitsInBlock.Select(v =>
                {
                    containers.TryGetValue(v.ContainerId, out var cont);
                    var moveEvent = v.Movements
                        .Where(m => m.EventType == "MOVE_TO_VIRTUAL")
                        .OrderByDescending(m => m.OccurredAt)
                        .FirstOrDefault();
                    DateTime? enteredWorkshopDate = moveEvent?.OccurredAt;

                    string typeDesc = cont is not null
                        ? cont.Type switch
                        {
                            Domain.Enums.ContainerType.Reefer => "Container lạnh (Reefer)",
                            Domain.Enums.ContainerType.Tank => "Container bồn (Tank)",
                            Domain.Enums.ContainerType.OpenTop => "Container mở nóc (Open Top)",
                            Domain.Enums.ContainerType.FlatRack => "Container mặt bằng (Flat Rack)",
                            _ => "Container khô tiêu chuẩn (GP)"
                        }
                        : string.Empty;

                    string? gradeDesc = FormatGrade(v.InSurveyGrade);
                    string? statusDesc = v.Status switch
                    {
                        Domain.Enums.ContainerVisitStatus.StackedInYard => "Đang lưu xưởng",
                        Domain.Enums.ContainerVisitStatus.GatedIn => "Mới nhập cổng",
                        _ => "Đang lưu xưởng"
                    };

                    int dwellDays = Math.Max(1, (int)(DateTime.UtcNow - v.GateInDate).TotalDays);
                    int activeHolds = v.Holds.Count(h => h.IsActive);

                    return new VirtualContainerItemDto(
                        v.Id,
                        v.ContainerId,
                        cont?.Number.Value ?? string.Empty,
                        cont?.IsoCode ?? string.Empty,
                        cont?.Size ?? 20,
                        typeDesc,
                        cont?.LineOperator ?? v.LineOperator,
                        gradeDesc,
                        statusDesc,
                        v.GateInDate,
                        enteredWorkshopDate,
                        dwellDays,
                        activeHolds,
                        v.InDamageNotes
                    );
                }).ToList();

                return new YardBlockDto(
                    b.Id,
                    b.BlockCode,
                    b.Name,
                    b.MaxBay,
                    b.MaxRow,
                    b.MaxTier,
                    b.HasReeferPower,
                    b.IsVirtual,
                    b.MaxCapacity,
                    totalContainers,
                    tierStats,
                    virtualContainers
                );
            }
            else
            {
                var slotsInBlock = slots.Where(s => s.BlockId == b.Id).ToList();
                var occupiedSlots = slotsInBlock
                    .Where(s => s.IsOccupied && s.CurrentContainerId.HasValue && activeContainerIds.Contains(s.CurrentContainerId.Value))
                    .ToList();
                int totalContainers = occupiedSlots.Select(s => s.CurrentContainerId!.Value).Distinct().Count();

                var maxTier = b.MaxTier ?? 4;
                var tierStats = Enumerable.Range(1, maxTier).Select(tier =>
                {
                    int countInTier = occupiedSlots
                        .Where(s => s.Tier == tier)
                        .Select(s => s.CurrentContainerId!.Value)
                        .Distinct()
                        .Count();
                    return new BlockTierStatDto(tier, countInTier);
                }).ToList();

                var virtualContainers = Array.Empty<VirtualContainerItemDto>();

                return new YardBlockDto(
                    b.Id,
                    b.BlockCode,
                    b.Name,
                    b.MaxBay,
                    b.MaxRow,
                    b.MaxTier,
                    b.HasReeferPower,
                    b.IsVirtual,
                    b.MaxCapacity,
                    totalContainers,
                    tierStats,
                    virtualContainers
                );
            }
        }).ToList();

        var slotDtos = slots.Select(s =>
        {
            var blockCode = blockDict.TryGetValue(s.BlockId, out var blk) ? blk.BlockCode : string.Empty;
            Container? cont = null;
            ContainerVisit? visit = null;
            if (s.CurrentContainerId.HasValue)
            {
                containers.TryGetValue(s.CurrentContainerId.Value, out cont);
                visits.TryGetValue(s.CurrentContainerId.Value, out visit);
            }

            string? colorScheme = null;
            if (cont is not null)
            {
                colorScheme = cont.Type switch
                {
                    Domain.Enums.ContainerType.Reefer => "blue",
                    Domain.Enums.ContainerType.Tank => "red",
                    Domain.Enums.ContainerType.OpenTop or Domain.Enums.ContainerType.FlatRack => "amber",
                    _ => "green"
                };
            }

            string? typeDesc = cont is not null
                ? cont.Type switch
                {
                    Domain.Enums.ContainerType.Reefer => "Container lạnh (Reefer)",
                    Domain.Enums.ContainerType.Tank => "Container bồn (Tank)",
                    Domain.Enums.ContainerType.OpenTop => "Container mở nóc (Open Top)",
                    Domain.Enums.ContainerType.FlatRack => "Container mặt bằng (Flat Rack)",
                    _ => "Container khô tiêu chuẩn (GP)"
                }
                : null;

            string? gradeDesc = visit is not null ? FormatGrade(visit.InSurveyGrade) : (cont is not null ? FormatGrade(cont.Grade) : null);
            string? statusDesc = visit?.Status switch
            {
                Domain.Enums.ContainerVisitStatus.StackedInYard => "Đang lưu bãi",
                Domain.Enums.ContainerVisitStatus.GatedIn => "Mới nhập cổng",
                Domain.Enums.ContainerVisitStatus.Allocated => "Đã cấp lệnh",
                _ => cont is not null ? "Đang lưu bãi" : null
            };

            int? dwellDays = visit is not null ? Math.Max(1, (int)(DateTime.UtcNow - visit.GateInDate).TotalDays) : null;
            int activeHolds = visit?.Holds.Count(h => h.IsActive) ?? 0;

            return new YardSlotDto(
                s.Id,
                s.BlockId,
                blockCode,
                s.Bay,
                s.Row,
                s.Tier,
                s.IsOccupied,
                s.CurrentContainerId,
                cont?.Number.Value,
                cont?.IsoCode,
                cont?.Size,
                cont?.LineOperator,
                colorScheme,
                visit?.Id,
                typeDesc,
                gradeDesc,
                statusDesc,
                visit?.GateInDate,
                dwellDays,
                visit?.InVehicle?.TractorNo,
                visit?.InVehicle?.DriverName,
                visit?.InDamageNotes,
                activeHolds
            );
        }).ToList();

        return Result.Success(new YardLayoutResponse(blockDtos, slotDtos));
    }

    private static string? FormatGrade(Domain.Enums.ContainerGrade? grade) => grade switch
    {
        Domain.Enums.ContainerGrade.GradeA => "Grade A",
        Domain.Enums.ContainerGrade.GradeB => "Grade B",
        Domain.Enums.ContainerGrade.GradeC => "Grade C",
        Domain.Enums.ContainerGrade.GradeD_Damaged => "Grade D",
        Domain.Enums.ContainerGrade.GradeE_Scrap => "Grade E",
        _ => grade.HasValue ? $"Grade {grade}" : null
    };
}
