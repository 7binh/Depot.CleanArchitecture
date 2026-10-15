namespace Depot.CleanArchitecture.Application.Features.Yard.RelocateContainer;

using System.Data;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class RelocateContainerCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RelocateContainerCommand, Result<RelocateContainerResponse>>
{
    private const int MaxDeadlockRetries = 2;

    public async Task<Result<RelocateContainerResponse>> HandleAsync(
        RelocateContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 0; attempt <= MaxDeadlockRetries; attempt++)
            {
                try
                {
                    return await ExecuteRelocateAsync(command, cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Phân biệt lỗi tranh chấp ô bãi hay container
                    var isSlotConflict = ex.Entries.Any(e => e.Entity is YardSlot);
                    if (isSlotConflict)
                    {
                        return Result.Failure<RelocateContainerResponse>(
                            Error.Conflict("YardSlot.AlreadyOccupied", "Ô bãi đích đã có container khác chiếm dụng!"));
                    }

                    var isVisitConflict = ex.Entries.Any(e => e.Entity is ContainerVisit);
                    if (isVisitConflict)
                    {
                        return Result.Failure<RelocateContainerResponse>(
                            Error.Conflict("ContainerVisit.ConcurrentConflict", "Lượt lưu bãi hoặc vị trí container đã bị thay đổi bởi thao tác khác đồng thời. Vui lòng tải lại dữ liệu!"));
                    }

                    return Result.Failure<RelocateContainerResponse>(
                        Error.Conflict("Yard.ConcurrentConflict", "Xung đột dữ liệu đồng thời khi đảo chuyển bãi. Vui lòng thử lại!"));
                }
                catch (Exception ex) when (IsDeadlockException(ex))
                {
                    if (attempt < MaxDeadlockRetries)
                    {
                        dbContext.ChangeTracker.Clear();
                        await Task.Delay(50 * (attempt + 1), cancellationToken);
                        continue;
                    }

                    return Result.Failure<RelocateContainerResponse>(
                        Error.Failure("Database.Deadlock", "Hệ thống cơ sở dữ liệu quá tải do tranh chấp khóa. Vui lòng thử lại sau giây lát!"));
                }
            }

            return Result.Failure<RelocateContainerResponse>(
                Error.Failure("Database.Deadlock", "Hệ thống cơ sở dữ liệu quá tải do tranh chấp khóa. Vui lòng thử lại sau giây lát!"));
        });
    }

    private async Task<Result<RelocateContainerResponse>> ExecuteRelocateAsync(
        RelocateContainerCommand command,
        CancellationToken cancellationToken)
    {
        var isRelational = dbContext.Database.IsRelational();
        IDbContextTransaction? transaction = null;

        if (isRelational)
        {
            transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        }

        try
        {
            var visit = await dbContext.ContainerVisits
                .Include(v => v.Holds)
                .Include(v => v.Movements)
                .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

            if (visit is null)
            {
                return Result.Failure<RelocateContainerResponse>(
                    Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt lưu bãi '{command.VisitId}'!"));
            }

            if (visit.Status is ContainerVisitStatus.GatedOut or ContainerVisitStatus.Completed)
            {
                return Result.Failure<RelocateContainerResponse>(
                    Error.Validation("Visit.InvalidStatus", $"Không thể đảo chuyển container ở trạng thái '{visit.Status}'!"));
            }

            var container = await dbContext.Containers
                .FirstOrDefaultAsync(c => c.Id == visit.ContainerId, cancellationToken);

            if (container is null)
            {
                return Result.Failure<RelocateContainerResponse>(
                    Error.NotFound("Container.NotFound", "Không tìm thấy dữ liệu container tương ứng!"));
            }

            // 1. Kiểm tra Khóa an toàn di chuyển (Safety Hold Rule)
            var safetyRule = new SafetyHoldPreventsMovementRule(visit.Holds);
            if (safetyRule.IsBroken())
            {
                return Result.Failure<RelocateContainerResponse>(
                    Error.Validation(safetyRule.RuleCode, safetyRule.Message));
            }

            // 2. Kiểm tra slot nguồn hiện tại & an toàn tầng trên (Upper Tier Rule)
            YardSlot? sourceSlot = null;
            YardBlock? sourceBlock = null;

            if (visit.CurrentSlotId.HasValue)
            {
                sourceSlot = await dbContext.YardSlots.FirstOrDefaultAsync(s => s.Id == visit.CurrentSlotId.Value, cancellationToken);
                if (sourceSlot is not null)
                {
                    sourceBlock = await dbContext.YardBlocks.FirstOrDefaultAsync(b => b.Id == sourceSlot.BlockId, cancellationToken);

                    var upperOccupiedSlotsCount = await dbContext.YardSlots.CountAsync(
                        s => s.BlockId == sourceSlot.BlockId &&
                             s.Bay == sourceSlot.Bay &&
                             s.Row == sourceSlot.Row &&
                             s.Tier > sourceSlot.Tier &&
                             s.IsOccupied,
                        cancellationToken);

                    var upperTierRule = new UpperTierContainerMustBeRelocatedFirstRule(sourceSlot.Tier, upperOccupiedSlotsCount);
                    if (upperTierRule.IsBroken())
                    {
                        return Result.Failure<RelocateContainerResponse>(
                            Error.Validation(upperTierRule.RuleCode, upperTierRule.Message));
                    }
                }
            }
            else if (visit.CurrentBlockId.HasValue)
            {
                sourceBlock = await dbContext.YardBlocks.FirstOrDefaultAsync(b => b.Id == visit.CurrentBlockId.Value, cancellationToken);
            }

            // 3. Kiểm tra Block đích
            var targetBlock = await dbContext.YardBlocks.FirstOrDefaultAsync(b => b.Id == command.TargetBlockId, cancellationToken);
            if (targetBlock is null)
            {
                return Result.Failure<RelocateContainerResponse>(
                    Error.NotFound("YardBlock.NotFound", "Không tìm thấy khu vực bãi (Block) đích!"));
            }

            string fromCoordinates = sourceSlot is not null
                ? $"{sourceBlock?.BlockCode}-{sourceSlot.Bay:D2}-{sourceSlot.Row:D2}-{sourceSlot.Tier:D2}"
                : sourceBlock?.BlockCode ?? "NONE";

            Guid? targetSlotId = null;
            string toCoordinates;
            bool isVirtual = targetBlock.IsVirtual;

            if (targetBlock.IsVirtual)
            {
                // 4. Di chuyển vào Block ảo (Physical -> Virtual hoặc Virtual -> Virtual)
                if (sourceSlot is null && visit.CurrentBlockId == targetBlock.Id)
                {
                    return Result.Failure<RelocateContainerResponse>(
                        Error.Validation("Yard.SameBlock", $"Container hiện đã ở trong khu vực bãi '{targetBlock.BlockCode}'!"));
                }

                var currentCount = await dbContext.ContainerVisits.CountAsync(
                    v => v.CurrentBlockId == targetBlock.Id &&
                         v.Status != ContainerVisitStatus.GatedOut &&
                         v.Status != ContainerVisitStatus.Completed,
                    cancellationToken);

                var capacityRule = new VirtualBlockCapacityRule(currentCount, targetBlock.MaxCapacity ?? int.MaxValue);
                if (capacityRule.IsBroken())
                {
                    return Result.Failure<RelocateContainerResponse>(
                        Error.Validation(capacityRule.RuleCode, capacityRule.Message));
                }

                toCoordinates = targetBlock.BlockCode;
            }
            else
            {
                // 5. Di chuyển vào Block vật lý (Physical -> Physical hoặc Virtual -> Physical)
                var targetSlot = await dbContext.YardSlots.FirstOrDefaultAsync(
                    s => s.BlockId == targetBlock.Id &&
                         s.Bay == command.TargetBay &&
                         s.Row == command.TargetRow &&
                         s.Tier == command.TargetTier,
                    cancellationToken);

                if (targetSlot is null)
                {
                    return Result.Failure<RelocateContainerResponse>(
                        Error.NotFound("YardSlot.NotFound", $"Không tìm thấy ô bãi đích tại {targetBlock.BlockCode}-{command.TargetBay:D2}-{command.TargetRow:D2}-{command.TargetTier:D2}!"));
                }

                if (targetSlot.IsOccupied)
                {
                    return Result.Failure<RelocateContainerResponse>(
                        Error.Conflict("YardSlot.AlreadyOccupied", "Ô bãi đích đã có container khác chiếm dụng!"));
                }

                // Kiểm tra quy tắc giàn điện lạnh (Reefer Rule)
                var reeferRule = new ReeferYardAssignmentRule(container.IsReefer, targetBlock.HasReeferPower, targetSlot.Tier);
                if (reeferRule.IsBroken())
                {
                    return Result.Failure<RelocateContainerResponse>(
                        Error.Validation(reeferRule.RuleCode, reeferRule.Message));
                }

                // Kiểm tra an toàn tầng dưới nếu Tier > 1
                if (targetSlot.Tier > 1)
                {
                    var hasBottomSupport = await dbContext.YardSlots.AnyAsync(
                        s => s.BlockId == targetBlock.Id &&
                             s.Bay == targetSlot.Bay &&
                             s.Row == targetSlot.Row &&
                             s.Tier == targetSlot.Tier - 1 &&
                             s.IsOccupied,
                        cancellationToken);

                    var bottomSupportRule = new TierMustHaveBottomSupportRule(targetSlot.Tier, hasBottomSupport);
                    if (bottomSupportRule.IsBroken())
                    {
                        return Result.Failure<RelocateContainerResponse>(
                            Error.Validation(bottomSupportRule.RuleCode, bottomSupportRule.Message));
                    }
                }

                // Kiểm tra luật va chạm Bay chẵn/lẻ (Even/Odd Bay Collision)
                var targetCoordResult = Domain.ValueObjects.SlotCoordinate.Create(targetSlot.Bay, targetSlot.Row, targetSlot.Tier);
                if (targetCoordResult.IsSuccess)
                {
                    var occupiedSlotsInSameRowAndTier = await dbContext.YardSlots
                        .Where(s => s.BlockId == targetBlock.Id &&
                                    s.Row == targetSlot.Row &&
                                    s.Tier == targetSlot.Tier &&
                                    s.Id != targetSlot.Id &&
                                    s.IsOccupied)
                        .ToListAsync(cancellationToken);

                    var collisionRule = new EvenOddBayCollisionRule(targetCoordResult.Value!, container.Size, occupiedSlotsInSameRowAndTier);
                    if (collisionRule.IsBroken())
                    {
                        return Result.Failure<RelocateContainerResponse>(
                            Error.Validation(collisionRule.RuleCode, collisionRule.Message));
                    }
                }

                targetSlot.Occupy(container.Id, container.Size);
                targetSlotId = targetSlot.Id;
                toCoordinates = $"{targetBlock.BlockCode}-{targetSlot.Bay:D2}-{targetSlot.Row:D2}-{targetSlot.Tier:D2}";
            }

            // 6. Thực hiện chuyển dịch nguyên tử
            sourceSlot?.Release();
            visit.Relocate(targetSlotId, targetBlock.Id, command.ActorId, command.Reason);

            if (visit.Movements.Count > 0)
            {
                dbContext.MovementEvents.Add(visit.Movements.Last());
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Result.Success(new RelocateContainerResponse(
                visit.Id,
                container.Number.Value,
                sourceBlock?.BlockCode,
                fromCoordinates,
                targetBlock.BlockCode,
                toCoordinates,
                isVirtual,
                DateTime.UtcNow));
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static bool IsDeadlockException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.GetType().Name == "SqlException")
            {
                var numberProp = current.GetType().GetProperty("Number");
                if (numberProp?.GetValue(current) is int number && number == 1205)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
