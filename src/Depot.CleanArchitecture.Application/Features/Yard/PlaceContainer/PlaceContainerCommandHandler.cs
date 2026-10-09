namespace Depot.CleanArchitecture.Application.Features.Yard.PlaceContainer;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class PlaceContainerCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<PlaceContainerCommand, Result<PlaceContainerResponse>>
{
    public async Task<Result<PlaceContainerResponse>> HandleAsync(
        PlaceContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.ContainerVisits
            .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

        if (visit is null)
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt vào bãi với ID '{command.VisitId}'!"));
        }

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.Id == visit.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.NotFound("Container.NotFound", "Không tìm thấy thông tin container của lượt vào bãi này!"));
        }

        var block = await dbContext.YardBlocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.NotFound("YardBlock.NotFound", $"Không tìm thấy khu bãi (block) với ID '{command.BlockId}'!"));
        }

        var coordResult = SlotCoordinate.Create(command.Bay, command.Row, command.Tier);
        if (coordResult.IsFailure)
        {
            return Result.Failure<PlaceContainerResponse>(coordResult.Error);
        }

        var coord = coordResult.Value!;

        // 1. Kiểm tra giới hạn block
        if (coord.Bay > block.MaxBay || coord.Row > block.MaxRow || coord.Tier > block.MaxTier)
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.Validation("Yard.OutOfBounds", $"Tọa độ {coord} vượt quá giới hạn của Block {block.BlockCode} ({block.MaxBay} Bay x {block.MaxRow} Row x {block.MaxTier} Tier)!"));
        }

        // 2. Tìm hoặc tạo slot tại vị trí chỉ định
        var slot = await dbContext.YardSlots
            .FirstOrDefaultAsync(s => s.BlockId == block.Id &&
                                      s.Bay == coord.Bay &&
                                      s.Row == coord.Row &&
                                      s.Tier == coord.Tier, cancellationToken);

        if (slot is null)
        {
            slot = YardSlot.Create(command.TenantId, block.Id, coord.Bay, coord.Row, coord.Tier);
            dbContext.YardSlots.Add(slot);
        }
        else if (slot.IsOccupied)
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.Conflict("Yard.SlotOccupied", $"Ô bãi {coord} tại Block {block.BlockCode} đã có container chiếm dụng!"));
        }

        // 3. Kiểm tra luật đỡ tầng dưới nếu Tier > 1
        if (coord.Tier > 1)
        {
            var lowerSlot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.BlockId == block.Id &&
                                          s.Bay == coord.Bay &&
                                          s.Row == coord.Row &&
                                          s.Tier == coord.Tier - 1, cancellationToken);

            bool hasBottomSupport = lowerSlot is not null && lowerSlot.IsOccupied;
            var supportRule = new TierMustHaveBottomSupportRule(coord.Tier, hasBottomSupport);
            if (supportRule.IsBroken())
            {
                return Result.Failure<PlaceContainerResponse>(
                    Error.Validation(supportRule.RuleCode, supportRule.Message));
            }
        }

        // 4. Kiểm tra luật va chạm Bay chẵn/lẻ (Even/Odd Bay Collision)
        var occupiedSlotsInSameRowAndTier = await dbContext.YardSlots
            .Where(s => s.BlockId == block.Id &&
                        s.Row == coord.Row &&
                        s.Tier == coord.Tier &&
                        s.IsOccupied)
            .ToListAsync(cancellationToken);

        var collisionRule = new EvenOddBayCollisionRule(coord, container.Size, occupiedSlotsInSameRowAndTier);
        if (collisionRule.IsBroken())
        {
            return Result.Failure<PlaceContainerResponse>(
                Error.Validation(collisionRule.RuleCode, collisionRule.Message));
        }

        // 5. Thỏa mãn -> Chiếm ô bãi và gán cho Visit
        slot.Occupy(container.Id, container.Size);
        visit.AssignSlot(slot.Id, block.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PlaceContainerResponse(
            visit.Id,
            slot.Id,
            $"{block.BlockCode}-{coord}");

        return Result.Success(response);
    }
}
