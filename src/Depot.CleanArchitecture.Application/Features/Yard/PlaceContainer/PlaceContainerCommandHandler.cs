namespace Depot.CleanArchitecture.Application.Features.Yard.PlaceContainer;

using System.Data;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class PlaceContainerCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<PlaceContainerCommand, Result<PlaceContainerResponse>>
{
    private const int MaxDeadlockRetries = 2;
    private const string DefaultActorId = "YARD_DISPATCHER";

    public async Task<Result<PlaceContainerResponse>> HandleAsync(
        PlaceContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var coordResult = SlotCoordinate.Create(command.Bay, command.Row, command.Tier);
        if (coordResult.IsFailure)
        {
            return Result.Failure<PlaceContainerResponse>(coordResult.Error);
        }

        var coord = coordResult.Value!;

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 0; attempt <= MaxDeadlockRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isRelational = dbContext.Database.IsRelational();
                IDbContextTransaction? transaction = null;

                try
                {
                    if (isRelational)
                    {
                        transaction = await dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable,
                            cancellationToken);

                        if (IsSqlServer(dbContext))
                        {
                            var visitLock = $"Place_Visit_{command.TenantId}_{command.VisitId}";
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "DECLARE @res int; EXEC @res = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; IF @res < 0 THROW 50002, 'Unable to acquire Place visit applock', 1;",
                                new object[] { visitLock },
                                cancellationToken);

                            var rowLock = $"Place_Row_{command.TenantId}_{command.BlockId}_{coord.Row}_{coord.Tier}";
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "DECLARE @res int; EXEC @res = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; IF @res < 0 THROW 50003, 'Unable to acquire Place row applock', 1;",
                                new object[] { rowLock },
                                cancellationToken);
                        }
                    }

                    // 1. Đọc và xác minh ContainerVisit
                    var visit = await dbContext.ContainerVisits
                        .Include(v => v.Movements)
                        .FirstOrDefaultAsync(v => v.Id == command.VisitId, cancellationToken);

                    if (visit is null)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt vào bãi với ID '{command.VisitId}'!"));
                    }

                    // 2. Xác minh Visit đang ở trạng thái GatedIn và chưa có slot/block
                    if (visit.Status != ContainerVisitStatus.GatedIn || visit.CurrentSlotId != null || visit.CurrentBlockId != null)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Conflict("Visit.InvalidStatus", $"Lượt vào bãi '{visit.Id}' không ở trạng thái chờ hạ bãi (GatedIn) hoặc đã được xếp chỗ trước đó!"));
                    }

                    // 3. Đọc thông tin Container
                    var container = await dbContext.Containers
                        .FirstOrDefaultAsync(c => c.Id == visit.ContainerId, cancellationToken);

                    if (container is null)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.NotFound("Container.NotFound", "Không tìm thấy thông tin container của lượt vào bãi này!"));
                    }

                    // 4. Kiểm tra kích thước container (chỉ hỗ trợ 20ft và 40ft; từ chối 45ft)
                    if (container.Size is not (20 or 40))
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Validation(
                                "Yard.UnsupportedContainerSize",
                                $"Hiện tại bãi chưa hỗ trợ hạ bãi tự động cho container kích thước {container.Size}ft (chỉ hỗ trợ 20ft và 40ft). Vui lòng chuyển sang bãi chuyên dụng!"));
                    }

                    // 5. Đọc thông tin Block
                    var block = await dbContext.YardBlocks
                        .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

                    if (block is null)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.NotFound("YardBlock.NotFound", $"Không tìm thấy khu bãi (block) với ID '{command.BlockId}'!"));
                    }

                    if (block.IsVirtual)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Validation("YardBlock.VirtualNotSupported", "Không thể hạ container có tọa độ ô vào khu vực bãi ảo!"));
                    }

                    // 6. Kiểm tra giới hạn block
                    if (coord.Bay > block.MaxBay || coord.Row > block.MaxRow || coord.Tier > block.MaxTier)
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Validation("Yard.OutOfBounds", $"Tọa độ {coord} vượt quá giới hạn của Block {block.BlockCode} ({block.MaxBay} Bay x {block.MaxRow} Row x {block.MaxTier} Tier)!"));
                    }

                    // 7. Kiểm tra quy tắc giàn điện lạnh (Reefer Stacking Rule)
                    var reeferRule = new ReeferYardAssignmentRule(container.IsReefer, block.HasReeferPower, coord.Tier);
                    if (reeferRule.IsBroken())
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Validation(reeferRule.RuleCode, reeferRule.Message));
                    }

                    // 8. Tìm hoặc tạo slot tại vị trí chỉ định (Dynamic Slot Creation)
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
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Conflict("Yard.SlotOccupied", $"Ô bãi {coord} tại Block {block.BlockCode} đã có container chiếm dụng!"));
                    }

                    // 9. Kiểm tra luật đỡ tầng dưới nếu Tier > 1
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
                            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                            return Result.Failure<PlaceContainerResponse>(
                                Error.Validation(supportRule.RuleCode, supportRule.Message));
                        }
                    }

                    // 10. Kiểm tra luật va chạm Bay chẵn/lẻ (Even/Odd Bay Collision)
                    var occupiedSlotsInSameRowAndTier = await dbContext.YardSlots
                        .Where(s => s.BlockId == block.Id &&
                                    s.Row == coord.Row &&
                                    s.Tier == coord.Tier &&
                                    s.IsOccupied)
                        .ToListAsync(cancellationToken);

                    var collisionRule = new EvenOddBayCollisionRule(coord, container.Size, occupiedSlotsInSameRowAndTier);
                    if (collisionRule.IsBroken())
                    {
                        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<PlaceContainerResponse>(
                            Error.Validation(collisionRule.RuleCode, collisionRule.Message));
                    }

                    // 11. Chiếm ô bãi và gán cho Visit
                    slot.Occupy(container.Id, container.Size);
                    visit.AssignSlot(slot.Id, block.Id);

                    // 12. Ghi nhận sự kiện di chuyển PLACE_YARD nguyên tử
                    var movement = MovementEvent.Create(
                        command.TenantId,
                        visit.Id,
                        "PLACE_YARD",
                        fromSlotId: null,
                        toSlotId: slot.Id,
                        actorId: DefaultActorId,
                        reason: $"Hạ bãi container tại {block.BlockCode}-{coord}");

                    visit.AddMovement(movement);
                    dbContext.MovementEvents.Add(movement);

                    // 13. SaveChanges và Commit
                    await dbContext.SaveChangesAsync(cancellationToken);

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    var response = new PlaceContainerResponse(
                        visit.Id,
                        slot.Id,
                        $"{block.BlockCode}-{coord}");

                    return Result.Success(response);
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    return Result.Failure<PlaceContainerResponse>(
                        Error.Conflict("Yard.SlotOccupied", "Ô bãi đã có container khác chiếm dụng đồng thời!"));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    return Result.Failure<PlaceContainerResponse>(
                        Error.Conflict("Yard.SlotOccupied", "Ô bãi đã có container khác chiếm dụng đồng thời!"));
                }
                catch (Exception ex) when (IsDeadlockException(ex) && attempt < MaxDeadlockRetries)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    dbContext.ChangeTracker.Clear();
                    await Task.Delay(50 * (attempt + 1), cancellationToken);
                    continue;
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

            return Result.Failure<PlaceContainerResponse>(
                Error.Failure("Database.Deadlock", "Hệ thống quá tải do tranh chấp khóa bãi. Vui lòng thử lại!"));
        });
    }

    private static bool IsSqlServer(IAppDbContext context)
    {
        return context.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
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

    private static bool IsDuplicateKeyException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.GetType().Name == "SqlException")
            {
                var numberProp = current.GetType().GetProperty("Number");
                if (numberProp?.GetValue(current) is int number && (number == 2601 || number == 2627))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
