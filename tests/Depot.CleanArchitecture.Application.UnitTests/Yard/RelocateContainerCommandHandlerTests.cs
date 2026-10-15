namespace Depot.CleanArchitecture.Application.UnitTests.Yard;

using Depot.CleanArchitecture.Application.Features.Yard.RelocateContainer;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class RelocateContainerCommandHandlerTests
{
    [Fact]
    public async Task Relocate_PhysicalToPhysical_WhenValid_ShouldSucceedAndMoveSlot()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var targetSlot = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot1.Occupy(cont.Id, 20);

        block.AddSlot(slot1);
        block.AddSlot(targetSlot);
        db.YardBlocks.Add(block);
        db.YardSlots.AddRange(slot1, targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignSlot(slot1.Id, block.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, block.Id, 3, 1, 1, "DIEUDO-01", "Chuyen vi tri");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVirtualBlock.Should().BeFalse();
        result.Value.FromCoordinates.Should().Be("A-01-01-01");
        result.Value.ToCoordinates.Should().Be("A-03-01-01");
        slot1.IsOccupied.Should().BeFalse();
        targetSlot.IsOccupied.Should().BeTrue();
        visit.CurrentSlotId.Should().Be(targetSlot.Id);
        visit.Movements.Should().ContainSingle(m => m.EventType == "RELOCATE_YARD" && m.FromSlotId == slot1.Id && m.ToSlotId == targetSlot.Id);
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_ShouldSucceedAndOccupyTargetSlot()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var physicalBlock = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var targetSlot = YardSlot.Create(tenantId, physicalBlock.Id, 1, 1, 1);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        physicalBlock.AddSlot(targetSlot);
        db.YardBlocks.AddRange(virtualBlock, physicalBlock);
        db.YardSlots.Add(targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeB, "Tray mop nhe");
        visit.AssignVirtualBlock(virtualBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, physicalBlock.Id, 1, 1, 1, "DIEUDO-01", "Roi xuong MR ve bai");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVirtualBlock.Should().BeFalse();
        result.Value.FromCoordinates.Should().Be("MR");
        result.Value.ToCoordinates.Should().Be("A-01-01-01");
        targetSlot.IsOccupied.Should().BeTrue();
        visit.CurrentSlotId.Should().Be(targetSlot.Id);
        visit.CurrentBlockId.Should().Be(physicalBlock.Id);
        visit.Movements.Should().ContainSingle(m => m.EventType == "RELOCATE_YARD" && m.FromSlotId == null && m.ToSlotId == targetSlot.Id);
    }

    [Fact]
    public async Task Relocate_VirtualToVirtual_DifferentBlock_ShouldSucceed()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var mrBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var washBlock = YardBlock.CreateVirtual(tenantId, "WASH", "Khu rửa vỏ", maxCapacity: 5);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        db.YardBlocks.AddRange(mrBlock, washBlock);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignVirtualBlock(mrBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, washBlock.Id, 0, 0, 0, "DIEUDO-01", "Chuyen sang khu rua");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVirtualBlock.Should().BeTrue();
        result.Value.FromCoordinates.Should().Be("MR");
        result.Value.ToCoordinates.Should().Be("WASH");
        visit.CurrentSlotId.Should().BeNull();
        visit.CurrentBlockId.Should().Be(washBlock.Id);
        visit.Movements.Should().ContainSingle(m => m.EventType == "MOVE_TO_VIRTUAL" && m.FromSlotId == null && m.ToSlotId == null);
    }

    [Fact]
    public async Task Relocate_VirtualToVirtual_SameBlock_ShouldFailWithValidation()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var mrBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        db.YardBlocks.Add(mrBlock);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignVirtualBlock(mrBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, mrBlock.Id, 0, 0, 0, "DIEUDO-01", "Di chuyen vao chinh no");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.SameBlock");
    }

    [Fact]
    public async Task Relocate_WhenTargetSlotIsOccupied_ShouldFailWith409Conflict()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var targetSlot = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

        var num1 = ContainerNumber.Create("CSQU3054383").Value!;
        var num2 = ContainerNumber.Create("CSQU4000000").Value!;
        var cont1 = Container.Create(tenantId, num1, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont2 = Container.Create(tenantId, num2, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot1.Occupy(cont1.Id, 20);
        targetSlot.Occupy(cont2.Id, 20); // Đã bị chiếm dụng

        block.AddSlot(slot1);
        block.AddSlot(targetSlot);
        db.YardBlocks.Add(block);
        db.YardSlots.AddRange(slot1, targetSlot);
        db.Containers.AddRange(cont1, cont2);

        var visit1 = ContainerVisit.CreateGateIn(tenantId, cont1.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit1.AssignSlot(slot1.Id, block.Id);
        db.ContainerVisits.Add(visit1);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit1.Id, block.Id, 3, 1, 1, "DIEUDO-01", "Chuyen vao o da chiem");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("YardSlot.AlreadyOccupied");
    }

    [Fact]
    public async Task Relocate_WhenUpperTierIsOccupied_ShouldFailWithSafetyRule()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var slot2 = YardSlot.Create(tenantId, block.Id, 1, 1, 2); // tier 2
        var targetSlot = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

        var num1 = ContainerNumber.Create("CSQU3054383").Value!;
        var num2 = ContainerNumber.Create("CSQU4000000").Value!;
        var cont1 = Container.Create(tenantId, num1, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont2 = Container.Create(tenantId, num2, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot1.Occupy(cont1.Id, 20);
        slot2.Occupy(cont2.Id, 20);

        block.AddSlot(slot1);
        block.AddSlot(slot2);
        block.AddSlot(targetSlot);
        db.YardBlocks.Add(block);
        db.YardSlots.AddRange(slot1, slot2, targetSlot);
        db.Containers.AddRange(cont1, cont2);

        var visit1 = ContainerVisit.CreateGateIn(tenantId, cont1.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit1.AssignSlot(slot1.Id, block.Id);
        db.ContainerVisits.Add(visit1);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit1.Id, block.Id, 3, 1, 1, "DIEUDO-01", "Chuyen vi tri");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.UpperTierContainerMustBeRelocatedFirst");
    }

    [Fact]
    public async Task Relocate_ToVirtualBlock_WhenCapacityAvailable_ShouldSucceedAndFreeSlot()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot.Occupy(cont.Id, 20);
        block.AddSlot(slot);
        db.YardBlocks.AddRange(block, virtualBlock);
        db.YardSlots.Add(slot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignSlot(slot.Id, block.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, virtualBlock.Id, 0, 0, 0, "DIEUDO-01", "Vào xưởng sửa chữa");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        slot.IsOccupied.Should().BeFalse();
        visit.CurrentSlotId.Should().BeNull();
        visit.CurrentBlockId.Should().Be(virtualBlock.Id);
        visit.Movements.Should().ContainSingle(m => m.EventType == "MOVE_TO_VIRTUAL");
    }

    [Fact]
    public async Task Relocate_ToVirtualBlock_WhenCapacityExceeded_ShouldFail()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 1);

        var num1 = ContainerNumber.Create("CSQU3054383").Value!;
        var num2 = ContainerNumber.Create("CSQU4000000").Value!;
        var cont1 = Container.Create(tenantId, num1, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont2 = Container.Create(tenantId, num2, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot.Occupy(cont1.Id, 20);

        db.YardBlocks.AddRange(block, virtualBlock);
        db.YardSlots.Add(slot);
        db.Containers.AddRange(cont1, cont2);

        // Cont 2 đang chiếm dung lượng 1 của MR
        var visit2 = ContainerVisit.CreateGateIn(tenantId, cont2.Id, "ONE", VehicleInfo.Create("51C-2", "51R-2", "B", "090").Value!, ContainerGrade.GradeA, null);
        visit2.AssignVirtualBlock(virtualBlock.Id);

        var visit1 = ContainerVisit.CreateGateIn(tenantId, cont1.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit1.AssignSlot(slot.Id, block.Id);

        db.ContainerVisits.AddRange(visit1, visit2);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit1.Id, virtualBlock.Id, 0, 0, 0, "DIEUDO-01", "Vào xưởng sửa chữa khi đầy");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.VirtualBlockCapacityExceeded");
    }

    [Fact]
    public async Task Relocate_WhenSafetyHoldIsActive_ShouldFailWithSafetyRule()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var targetSlot = YardSlot.Create(tenantId, block.Id, 3, 1, 1);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        slot.Occupy(cont.Id, 20);
        block.AddSlot(slot);
        block.AddSlot(targetSlot);
        db.YardBlocks.Add(block);
        db.YardSlots.AddRange(slot, targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignSlot(slot.Id, block.Id);
        visit.ApplyHold("SAFETY_HOLD", "Nguy co nut vo nguy hiem", "ANTOAN");
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, block.Id, 3, 1, 1, "DIEUDO-01", "Chuyen vi tri");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.SafetyHoldPreventsMovement");
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_WhenReeferRuleBroken_ShouldFail()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var dryBlock = YardBlock.CreatePhysical(tenantId, "A", "Block Khô", 5, 5, 4, hasReeferPower: false);
        var targetSlot = YardSlot.Create(tenantId, dryBlock.Id, 1, 1, 1);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        // Container Lạnh (Reefer)
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Reefer, "22R1", 20, 2800m, 30480m, ContainerGrade.GradeA).Value!;

        dryBlock.AddSlot(targetSlot);
        db.YardBlocks.AddRange(virtualBlock, dryBlock);
        db.YardSlots.Add(targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignVirtualBlock(virtualBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, dryBlock.Id, 1, 1, 1, "DIEUDO-01", "Chuyen cont lanh vao block thuong");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.ReeferRequiresPowerReceptacle");
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_WhenTierBottomSupportMissing_ShouldFail()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var slotTier1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1); // trống, không có cont
        var targetSlotTier2 = YardSlot.Create(tenantId, block.Id, 1, 1, 2); // đặt vào tầng 2

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        block.AddSlot(slotTier1);
        block.AddSlot(targetSlotTier2);
        db.YardBlocks.AddRange(virtualBlock, block);
        db.YardSlots.AddRange(slotTier1, targetSlotTier2);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit.AssignVirtualBlock(virtualBlock.Id);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, block.Id, 1, 1, 2, "DIEUDO-01", "Dat tang 2 khong do day");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.TierMissingBottomSupport");
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_WhenEvenOddBayCollision_ShouldFail()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 10, 5, 4);

        // Đã có cont 20ft ở Bay 01, Row 01, Tier 01
        var slotBay1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        var slotBay2 = YardSlot.Create(tenantId, block.Id, 2, 1, 1); // Đích cho cont 40ft (Bay 02 bao phủ Bay 01 & 03)

        var num20 = ContainerNumber.Create("CSQU3054383").Value!;
        var num40 = ContainerNumber.Create("CSQU4000000").Value!;
        var cont20 = Container.Create(tenantId, num20, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont40 = Container.Create(tenantId, num40, "ONE", ContainerType.Dry, "42G1", 40, 3800m, 30480m, ContainerGrade.GradeA).Value!;

        slotBay1.Occupy(cont20.Id, 20);

        block.AddSlot(slotBay1);
        block.AddSlot(slotBay2);
        db.YardBlocks.AddRange(virtualBlock, block);
        db.YardSlots.AddRange(slotBay1, slotBay2);
        db.Containers.AddRange(cont20, cont40);

        var visit40 = ContainerVisit.CreateGateIn(tenantId, cont40.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeA, null);
        visit40.AssignVirtualBlock(virtualBlock.Id);
        db.ContainerVisits.Add(visit40);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit40.Id, block.Id, 2, 1, 1, "DIEUDO-01", "Chuyen cont 40ft va cham bay 1");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.EvenOddBayCollision");
    }

    [Fact]
    public async Task Relocate_VirtualToPhysical_PreservesConditionSurveyAndDamageNotes()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng M&R", maxCapacity: 10);
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 5, 5, 4);
        var targetSlot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);

        var num = ContainerNumber.Create("CSQU3054383").Value!;
        var cont = Container.Create(tenantId, num, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;

        block.AddSlot(targetSlot);
        db.YardBlocks.AddRange(virtualBlock, block);
        db.YardSlots.Add(targetSlot);
        db.Containers.Add(cont);

        var visit = ContainerVisit.CreateGateIn(tenantId, cont.Id, "ONE", VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!, ContainerGrade.GradeC, "Cont can han va gia co");
        visit.AssignVirtualBlock(virtualBlock.Id);
        var survey = ConditionSurvey.Create(tenantId, visit.Id, ContainerGrade.GradeC, "DAMAGED", ["HOLE"], "Thung vach cont", null, "SURVEYOR-01").Value!;
        visit.AddSurvey(survey);

        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new RelocateContainerCommandHandler(db);
        var command = new RelocateContainerCommand(visit.Id, block.Id, 1, 1, 1, "DIEUDO-01", "Xuat xuong ve bai");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        visit.InDamageNotes.Should().Be("Cont can han va gia co");
        visit.Surveys.Should().ContainSingle(s => s.Notes == "Thung vach cont");
        visit.InSurveyGrade.Should().Be(ContainerGrade.GradeC);
    }
}
