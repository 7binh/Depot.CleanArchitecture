namespace Depot.CleanArchitecture.Application.Features.Gate.GateIn;

using System.Data;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class GateInCommandHandler(IAppDbContext dbContext) 
    : ICommandHandler<GateInCommand, Result<GateInResponse>>
{
    private const int MaxDeadlockRetries = 2;
    private const string DefaultActorId = "GATE_CLERK";

    public async Task<Result<GateInResponse>> HandleAsync(
        GateInCommand command, 
        CancellationToken cancellationToken = default)
    {
        var containerNumberResult = ContainerNumber.Create(command.ContainerNumber);
        if (containerNumberResult.IsFailure)
        {
            return Result.Failure<GateInResponse>(containerNumberResult.Error);
        }

        var containerNumber = containerNumberResult.Value!;

        var vehicleResult = VehicleInfo.Create(
            command.TractorNo,
            command.TrailerNo,
            command.DriverName,
            command.DriverPhone);

        if (vehicleResult.IsFailure)
        {
            return Result.Failure<GateInResponse>(vehicleResult.Error);
        }

        var vehicle = vehicleResult.Value!;

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
                            IsolationLevel.ReadCommitted,
                            cancellationToken);

                        if (IsSqlServer(dbContext))
                        {
                            var lockResource = $"GateIn_{command.TenantId}_{containerNumber.Value}";
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "DECLARE @res int; EXEC @res = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; IF @res < 0 THROW 50001, 'Unable to acquire Gate-In applock', 1;",
                                new object[] { lockResource },
                                cancellationToken);
                        }
                    }

                    // 1. Tìm container qua value object
                    var container = await dbContext.Containers
                        .FirstOrDefaultAsync(c => c.Number == containerNumber, cancellationToken);

                    if (container is null)
                    {
                        var (tare, maxGross, isoCode) = Container.GetDefaultSpecs(command.Size, command.Type);

                        var createContainerResult = Container.Create(
                            command.TenantId,
                            containerNumber,
                            command.LineOperator,
                            command.Type,
                            isoCode,
                            command.Size,
                            tare,
                            maxGross,
                            command.Grade);

                        if (createContainerResult.IsFailure)
                        {
                            if (transaction is not null)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                            }
                            return Result.Failure<GateInResponse>(createContainerResult.Error);
                        }

                        container = createContainerResult.Value!;
                        dbContext.Containers.Add(container);
                    }

                    // 2. Kiểm tra xem container có đang ở bãi không (active visit)
                    var activeVisit = await dbContext.ContainerVisits
                        .FirstOrDefaultAsync(v => v.ContainerId == container.Id &&
                                                  v.Status != ContainerVisitStatus.GatedOut &&
                                                  v.Status != ContainerVisitStatus.Completed, cancellationToken);

                    if (activeVisit is not null)
                    {
                        if (transaction is not null)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                        }
                        return Result.Failure<GateInResponse>(
                            Error.Conflict("Visit.AlreadyActive", $"Container '{container.Number.Value}' hiện đang có lượt vào bãi chưa xuất!"));
                    }

                    // 3. Tạo ContainerVisit
                    var visit = ContainerVisit.CreateGateIn(
                        command.TenantId,
                        container.Id,
                        command.LineOperator,
                        vehicle,
                        command.Grade,
                        command.DamageNotes);

                    dbContext.ContainerVisits.Add(visit);

                    // 4. Tạo MovementEvent("GATE_IN") nguyên tử
                    var movement = MovementEvent.Create(
                        command.TenantId,
                        visit.Id,
                        "GATE_IN",
                        fromSlotId: null,
                        toSlotId: null,
                        actorId: DefaultActorId,
                        reason: "Tiếp nhận vỏ container vào bãi tại cổng IN");

                    visit.AddMovement(movement);
                    dbContext.MovementEvents.Add(movement);

                    await dbContext.SaveChangesAsync(cancellationToken);

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    var response = new GateInResponse(
                        visit.Id,
                        container.Id,
                        container.Number.Value,
                        visit.Status.ToString(),
                        visit.GateInDate);

                    return Result.Success(response);
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex) && IsContainerConflict(ex))
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    dbContext.ChangeTracker.Clear();

                    if (attempt < MaxDeadlockRetries)
                    {
                        await Task.Delay(50 * (attempt + 1), cancellationToken);
                        continue;
                    }

                    return Result.Failure<GateInResponse>(
                        Error.Conflict("Visit.AlreadyActive", $"Container '{containerNumber.Value}' đang được xử lý đồng thời bởi một lượt vào bãi khác!"));
                }
                catch (Exception ex) when (IsDeadlockException(ex))
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    dbContext.ChangeTracker.Clear();

                    if (attempt < MaxDeadlockRetries)
                    {
                        await Task.Delay(50 * (attempt + 1), cancellationToken);
                        continue;
                    }

                    return Result.Failure<GateInResponse>(
                        Error.Failure("Database.Deadlock", "Hệ thống cơ sở dữ liệu quá tải do tranh chấp khóa. Vui lòng thử lại sau giây lát!"));
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

            return Result.Failure<GateInResponse>(
                Error.Failure("Database.Deadlock", "Hệ thống cơ sở dữ liệu quá tải do tranh chấp khóa. Vui lòng thử lại sau giây lát!"));
        });
    }

    private static bool IsSqlServer(IAppDbContext dbContext)
    {
        return dbContext.Database.ProviderName?.EndsWith("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
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
                if (numberProp?.GetValue(current) is int number && (number is 2601 or 2627))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsContainerConflict(DbUpdateException ex)
    {
        return ex.Entries.Any(e => e.Entity is Container) ||
               ex.InnerException?.Message.Contains("Containers", StringComparison.OrdinalIgnoreCase) == true;
    }
}
