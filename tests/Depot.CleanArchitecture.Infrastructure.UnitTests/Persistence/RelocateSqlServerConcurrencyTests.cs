namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Persistence;

using System.Data;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Application.Features.Yard.RelocateContainer;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class RelocateSqlServerConcurrencyTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Depot_Concurrency_Test;Trusted_Connection=True;TrustServerCertificate=True;";

    private sealed class TestTenantProvider(Guid tenantId) : ITenantProvider
    {
        public string? TenantId => tenantId.ToString();
        public Guid CurrentTenantId => tenantId;
        public TenantInfo? CurrentTenant => null;
    }

    private static AppDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        var context = new AppDbContext(options, new TestTenantProvider(tenantId));
        return context;
    }

    private static ContainerNumber CreateValidContainerNumber(int seed)
    {
        var prefix = $"CSQU{seed:D6}";
        var map = new Dictionary<char, int>
        {
            {'C', 13}, {'S', 30}, {'Q', 28}, {'U', 32}
        };
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = prefix[i];
            int val = char.IsLetter(c) ? map[c] : (c - '0');
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        char check = rem == 10 ? '0' : (char)('0' + rem);
        return ContainerNumber.Create($"{prefix}{check}").Value!;
    }

    public RelocateSqlServerConcurrencyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var initContext = new AppDbContext(options);
        initContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Relocate_PhysicalToPhysical_OnRealSqlServer_SucceedsAtomically()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateContext(tenantId);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var slot2 = YardSlot.Create(tenantId, block.Id, 3, 1, 1);
        var cont = Container.Create(tenantId, CreateValidContainerNumber(100001), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot1.Occupy(cont.Id, 20);

        db.YardBlocks.Add(block);
        db.YardSlots.AddRange(slot1, slot2);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignSlot(slot1.Id, block.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, block.Id, 3, 1, 1, "OPERATOR-01", "Chuyen sang slot 3");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value!.ToCoordinates.Should().Be("A-03-01-01");
        result.Value.FromCoordinates.Should().Be("A-01-01-01");

        // Verify fresh state from database
        await using var verifyDb = CreateContext(tenantId);
        var dbSlot1 = await verifyDb.YardSlots.FindAsync([slot1.Id], TestContext.Current.CancellationToken);
        var dbSlot2 = await verifyDb.YardSlots.FindAsync([slot2.Id], TestContext.Current.CancellationToken);
        var dbVisit = await verifyDb.ContainerVisits.Include(v => v.Movements).FirstAsync(v => v.Id == visit.Id, TestContext.Current.CancellationToken);

        dbSlot1!.IsOccupied.Should().BeFalse();
        dbSlot2!.IsOccupied.Should().BeTrue();
        dbVisit.CurrentSlotId.Should().Be(slot2.Id);
        dbVisit.Movements.Should().ContainSingle(m => m.EventType == "RELOCATE_YARD" && m.FromSlotId == slot1.Id && m.ToSlotId == slot2.Id);
    }

    [Fact]
    public async Task Relocate_PhysicalToVirtual_OnRealSqlServer_SucceedsAndFreesSourceSlot()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateContext(tenantId);

        var physicalBlock = YardBlock.CreatePhysical(tenantId, "B", "Block B", 5, 5, 4);
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var slot = YardSlot.Create(tenantId, physicalBlock.Id, 1, 1, 1);
        var cont = Container.Create(tenantId, CreateValidContainerNumber(100002), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot.Occupy(cont.Id, 20);

        db.YardBlocks.AddRange(physicalBlock, virtualBlock);
        db.YardSlots.Add(slot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignSlot(slot.Id, physicalBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, virtualBlock.Id, 0, 0, 0, "OPERATOR-01", "Dua vao sua chua");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value!.IsVirtualBlock.Should().BeTrue();
        result.Value.ToCoordinates.Should().Be("MR");

        await using var verifyDb = CreateContext(tenantId);
        var dbSlot = await verifyDb.YardSlots.FindAsync([slot.Id], TestContext.Current.CancellationToken);
        var dbVisit = await verifyDb.ContainerVisits.Include(v => v.Movements).FirstAsync(v => v.Id == visit.Id, TestContext.Current.CancellationToken);

        dbSlot!.IsOccupied.Should().BeFalse();
        dbVisit.CurrentSlotId.Should().BeNull();
        dbVisit.CurrentBlockId.Should().Be(virtualBlock.Id);
        dbVisit.Movements.Should().ContainSingle(m => m.EventType == "MOVE_TO_VIRTUAL");
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_OnRealSqlServer_PreservesDamageStateAndOccupiesSlot()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateContext(tenantId);

        var physicalBlock = YardBlock.CreatePhysical(tenantId, "C", "Block C", 5, 5, 4);
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "WASH", "Khu ve sinh", maxCapacity: 10);
        var targetSlot = YardSlot.Create(tenantId, physicalBlock.Id, 1, 1, 1);
        var cont = Container.Create(tenantId, CreateValidContainerNumber(100003), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        db.YardBlocks.AddRange(physicalBlock, virtualBlock);
        db.YardSlots.Add(targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeB, "Tray xuoc hong cont");
        visit.AssignVirtualBlock(virtualBlock.Id);
        var survey = ConditionSurvey.Create(tenantId, visit.Id, ContainerGrade.GradeB, "GOOD", ["SCRATCH"], "Ghi chu giam dinh", null, "SURVEYOR-01").Value!;
        visit.AddSurvey(survey);

        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, physicalBlock.Id, 1, 1, 1, "OPERATOR-01", "Hoan tat ve sinh dua ve bai");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value!.IsVirtualBlock.Should().BeFalse();
        result.Value.FromCoordinates.Should().Be("WASH");
        result.Value.ToCoordinates.Should().Be("C-01-01-01");

        // BR-08: Surveys & damage notes must remain intact
        await using var verifyDb = CreateContext(tenantId);
        var dbVisit = await verifyDb.ContainerVisits
            .Include(v => v.Surveys)
            .Include(v => v.Movements)
            .FirstAsync(v => v.Id == visit.Id, TestContext.Current.CancellationToken);

        dbVisit.CurrentSlotId.Should().Be(targetSlot.Id);
        dbVisit.CurrentBlockId.Should().Be(physicalBlock.Id);
        dbVisit.InDamageNotes.Should().Be("Tray xuoc hong cont");
        dbVisit.Surveys.Should().ContainSingle(s => s.Notes == "Ghi chu giam dinh");
        dbVisit.Movements.Should().ContainSingle(m => m.EventType == "RELOCATE_YARD" && m.FromSlotId == null && m.ToSlotId == targetSlot.Id);
    }

    [Fact]
    public async Task Relocate_VirtualToVirtual_OnRealSqlServer_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        await using var db = CreateContext(tenantId);

        var mrBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var washBlock = YardBlock.CreateVirtual(tenantId, "WASH", "Khu rửa cont", maxCapacity: 10);
        var cont = Container.Create(tenantId, CreateValidContainerNumber(100004), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        db.YardBlocks.AddRange(mrBlock, washBlock);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignVirtualBlock(mrBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, washBlock.Id, 0, 0, 0, "OPERATOR-01", "Chuyen tu MR sang WASH");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value!.IsVirtualBlock.Should().BeTrue();
        result.Value.FromCoordinates.Should().Be("MR");
        result.Value.ToCoordinates.Should().Be("WASH");

        await using var verifyDb = CreateContext(tenantId);
        var dbVisit = await verifyDb.ContainerVisits.Include(v => v.Movements).FirstAsync(v => v.Id == visit.Id, TestContext.Current.CancellationToken);
        dbVisit.CurrentBlockId.Should().Be(washBlock.Id);
        dbVisit.CurrentSlotId.Should().BeNull();
        dbVisit.Movements.Should().ContainSingle(m => m.EventType == "MOVE_TO_VIRTUAL" && m.FromSlotId == null && m.ToSlotId == null);
    }

    [Fact]
    public async Task Relocate_ConcurrentRequests_CompetingForSameSlot_OneSucceedsAndOneReturns409Conflict()
    {
        var tenantId = Guid.NewGuid();
        await using (var db = CreateContext(tenantId))
        {
            var block = YardBlock.CreatePhysical(tenantId, "D", "Block D", 5, 5, 4);
            var slotSource1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
            var slotSource2 = YardSlot.Create(tenantId, block.Id, 1, 2, 1);
            var targetSlot = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

            var cont1 = Container.Create(tenantId, CreateValidContainerNumber(100005), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            var cont2 = Container.Create(tenantId, CreateValidContainerNumber(100006), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            slotSource1.Occupy(cont1.Id, 20);
            slotSource2.Occupy(cont2.Id, 20);

            db.YardBlocks.Add(block);
            db.YardSlots.AddRange(slotSource1, slotSource2, targetSlot);
            db.Containers.AddRange(cont1, cont2);

            var visit1 = ContainerVisit.CreateGateIn(tenantId, cont1.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
            visit1.AssignSlot(slotSource1.Id, block.Id);

            var visit2 = ContainerVisit.CreateGateIn(tenantId, cont2.Id, "ONE", VehicleInfo.Create("51C-2", "51R-2", "B", "091").Value!, ContainerGrade.GradeA, null);
            visit2.AssignSlot(slotSource2.Id, block.Id);

            db.ContainerVisits.AddRange(visit1, visit2);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Two concurrent requests using separate DbContext instances on real SQL Server
        var task1 = Task.Run(async () =>
        {
            await using var db1 = CreateContext(tenantId);
            var handler1 = new RelocateContainerCommandHandler(db1);
            var visit1 = await db1.ContainerVisits.FirstAsync(v => v.InVehicle.TractorNo == "51C-1");
            var block = await db1.YardBlocks.FirstAsync(b => b.BlockCode == "D");
            return await handler1.HandleAsync(new RelocateContainerCommand(visit1.Id, block.Id, 3, 1, 1, "OP-1", "Tranh chap slot 3"));
        });

        var task2 = Task.Run(async () =>
        {
            await using var db2 = CreateContext(tenantId);
            var handler2 = new RelocateContainerCommandHandler(db2);
            var visit2 = await db2.ContainerVisits.FirstAsync(v => v.InVehicle.TractorNo == "51C-2");
            var block = await db2.YardBlocks.FirstAsync(b => b.BlockCode == "D");
            return await handler2.HandleAsync(new RelocateContainerCommand(visit2.Id, block.Id, 3, 1, 1, "OP-2", "Tranh chap slot 3"));
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => r.IsFailure);

        successCount.Should().Be(1, "Exactly one request should claim the empty slot");
        failureCount.Should().Be(1, "The competing request must fail");

        var failure = results.First(r => r.IsFailure);
        failure.Error.Type.Should().Be(ErrorType.Conflict);
        failure.Error.Code.Should().Be("YardSlot.AlreadyOccupied");

        // Verify that in the database, exactly one container is in targetSlot
        await using var verifyDb = CreateContext(tenantId);
        var dbTargetSlot = await verifyDb.YardSlots.FirstAsync(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1, TestContext.Current.CancellationToken);
        dbTargetSlot.IsOccupied.Should().BeTrue();
        dbTargetSlot.CurrentContainerId.Should().NotBeNull();
    }

    [Fact]
    public async Task Relocate_ConcurrentRequests_MovingSameContainer_OneSucceedsAndOneDetectsConflict()
    {
        var tenantId = Guid.NewGuid();
        Guid visitId;
        Guid blockId;

        await using (var db = CreateContext(tenantId))
        {
            var block = YardBlock.CreatePhysical(tenantId, "E", "Block E", 5, 5, 4);
            var slotSource = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
            var slotTargetA = YardSlot.Create(tenantId, block.Id, 3, 1, 1);
            var slotTargetB = YardSlot.Create(tenantId, block.Id, 5, 1, 1);

            var cont = Container.Create(tenantId, CreateValidContainerNumber(100007), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            slotSource.Occupy(cont.Id, 20);

            db.YardBlocks.Add(block);
            db.YardSlots.AddRange(slotSource, slotTargetA, slotTargetB);
            db.Containers.Add(cont);

            var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
            visit.AssignSlot(slotSource.Id, block.Id);

            db.ContainerVisits.Add(visit);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            visitId = visit.Id;
            blockId = block.Id;
        }

        // Two concurrent requests moving the SAME container to different slots
        var task1 = Task.Run(async () =>
        {
            await using var db1 = CreateContext(tenantId);
            var handler1 = new RelocateContainerCommandHandler(db1);
            return await handler1.HandleAsync(new RelocateContainerCommand(visitId, blockId, 3, 1, 1, "OP-1", "Doi sang slot 3"));
        });

        var task2 = Task.Run(async () =>
        {
            await using var db2 = CreateContext(tenantId);
            var handler2 = new RelocateContainerCommandHandler(db2);
            return await handler2.HandleAsync(new RelocateContainerCommand(visitId, blockId, 5, 1, 1, "OP-2", "Doi sang slot 5"));
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);

        successCount.Should().BeGreaterThanOrEqualTo(1);

        // Verify consistent end state
        await using var verifyDb = CreateContext(tenantId);
        var finalVisit = await verifyDb.ContainerVisits.FindAsync([visitId], TestContext.Current.CancellationToken);
        finalVisit!.CurrentSlotId.Should().NotBeNull();
    }

    private sealed class FailOnMovementEventInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var db = eventData.Context;
            if (db?.ChangeTracker.Entries<MovementEvent>().Any() == true)
            {
                throw new InvalidOperationException("Simulated error saving MovementEvent!");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task Relocate_WhenMovementEventFails_RollsBackTransactionEntirely()
    {
        var tenantId = Guid.NewGuid();
        Guid visitId;
        Guid blockId;
        Guid sourceSlotId;
        Guid targetSlotId;

        await using (var db = CreateContext(tenantId))
        {
            var block = YardBlock.CreatePhysical(tenantId, "F", "Block F", 5, 5, 4);
            var slotSource = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
            var slotTarget = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

            var cont = Container.Create(tenantId, CreateValidContainerNumber(100008), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            slotSource.Occupy(cont.Id, 20);

            db.YardBlocks.Add(block);
            db.YardSlots.AddRange(slotSource, slotTarget);
            db.Containers.Add(cont);

            var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
            visit.AssignSlot(slotSource.Id, block.Id);

            db.ContainerVisits.Add(visit);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            visitId = visit.Id;
            blockId = block.Id;
            sourceSlotId = slotSource.Id;
            targetSlotId = slotTarget.Id;
        }

        // Configure context with interceptor that fails when saving MovementEvent
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new FailOnMovementEventInterceptor())
            .Options;

        await using (var failingDb = new AppDbContext(options, new TestTenantProvider(tenantId)))
        {
            var handler = new RelocateContainerCommandHandler(failingDb);
            var command = new RelocateContainerCommand(visitId, blockId, 3, 1, 1, "OP-1", "Di chuyen loi");

            var act = async () => await handler.HandleAsync(command, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Simulated error saving MovementEvent!*");
        }

        // Verify that in the database, the transaction was fully rolled back
        await using var verifyDb = CreateContext(tenantId);
        var sourceSlot = await verifyDb.YardSlots.FindAsync([sourceSlotId], TestContext.Current.CancellationToken);
        var targetSlot = await verifyDb.YardSlots.FindAsync([targetSlotId], TestContext.Current.CancellationToken);
        var visitAfter = await verifyDb.ContainerVisits
            .Include(v => v.Movements)
            .FirstAsync(v => v.Id == visitId, TestContext.Current.CancellationToken);

        sourceSlot!.IsOccupied.Should().BeTrue("Source slot must remain occupied after rollback");
        targetSlot!.IsOccupied.Should().BeFalse("Target slot must NOT be occupied after rollback");
        visitAfter.CurrentSlotId.Should().Be(sourceSlotId, "Visit must stay at source slot");
        visitAfter.Movements.Should().BeEmpty("No orphaned MovementEvent should exist after rollback");
    }

    private sealed class SqlException : Exception
    {
        public int Number => 1205;
        public SqlException() : base("Transaction was deadlocked on lock resources and has been chosen as the deadlock victim.") { }
    }

    private sealed class DeadlockThenSucceedInterceptor(int deadlockTimes) : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        private int _attempts = 0;
        public int ActualDeadlocks => _attempts;

        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<MovementEvent>().Any() == true)
            {
                if (_attempts < deadlockTimes)
                {
                    _attempts++;
                    throw new SqlException();
                }
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task Relocate_WhenSqlDeadlockOccurs_RetriesAndSucceedsWithoutDuplicateMovements()
    {
        var tenantId = Guid.NewGuid();
        Guid visitId;
        Guid blockId;
        Guid targetSlotId;

        await using (var db = CreateContext(tenantId))
        {
            var block = YardBlock.CreatePhysical(tenantId, "G", "Block G", 5, 5, 4);
            var slotSource = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
            var slotTarget = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

            var cont = Container.Create(tenantId, CreateValidContainerNumber(100009), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            slotSource.Occupy(cont.Id, 20);

            db.YardBlocks.Add(block);
            db.YardSlots.AddRange(slotSource, slotTarget);
            db.Containers.Add(cont);

            var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
            visit.AssignSlot(slotSource.Id, block.Id);

            db.ContainerVisits.Add(visit);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            visitId = visit.Id;
            blockId = block.Id;
            targetSlotId = slotTarget.Id;
        }

        var interceptor = new DeadlockThenSucceedInterceptor(deadlockTimes: 1);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using (var deadlockDb = new AppDbContext(options, new TestTenantProvider(tenantId)))
        {
            var handler = new RelocateContainerCommandHandler(deadlockDb);
            var command = new RelocateContainerCommand(visitId, blockId, 3, 1, 1, "OP-1", "Di chuyen co deadlock 1 lan");

            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.Should().BeTrue();
            interceptor.ActualDeadlocks.Should().Be(1, "Should have experienced exactly 1 deadlock before succeeding");
        }

        // Verify that in the database, exactly ONE MovementEvent exists and target slot is occupied
        await using var verifyDb = CreateContext(tenantId);
        var targetSlot = await verifyDb.YardSlots.FindAsync([targetSlotId], TestContext.Current.CancellationToken);
        var visitAfter = await verifyDb.ContainerVisits
            .Include(v => v.Movements)
            .FirstAsync(v => v.Id == visitId, TestContext.Current.CancellationToken);

        targetSlot!.IsOccupied.Should().BeTrue();
        visitAfter.CurrentSlotId.Should().Be(targetSlotId);
        visitAfter.Movements.Should().HaveCount(1, "Deadlock retry must NOT produce duplicate MovementEvents");
    }

    [Fact]
    public async Task Relocate_WhenSqlDeadlockExceedsMaxRetries_FailsWithDatabaseDeadlockError()
    {
        var tenantId = Guid.NewGuid();
        Guid visitId;
        Guid blockId;

        await using (var db = CreateContext(tenantId))
        {
            var block = YardBlock.CreatePhysical(tenantId, "H", "Block H", 5, 5, 4);
            var slotSource = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
            var slotTarget = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

            var cont = Container.Create(tenantId, CreateValidContainerNumber(100010), "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            slotSource.Occupy(cont.Id, 20);

            db.YardBlocks.Add(block);
            db.YardSlots.AddRange(slotSource, slotTarget);
            db.Containers.Add(cont);

            var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
            visit.AssignSlot(slotSource.Id, block.Id);

            db.ContainerVisits.Add(visit);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            visitId = visit.Id;
            blockId = block.Id;
        }

        var interceptor = new DeadlockThenSucceedInterceptor(deadlockTimes: 5); // exceeds MaxDeadlockRetries = 2
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using (var deadlockDb = new AppDbContext(options, new TestTenantProvider(tenantId)))
        {
            var handler = new RelocateContainerCommandHandler(deadlockDb);
            var command = new RelocateContainerCommand(visitId, blockId, 3, 1, 1, "OP-1", "Deadlock lien tuc");

            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Database.Deadlock");
        }
    }
}
