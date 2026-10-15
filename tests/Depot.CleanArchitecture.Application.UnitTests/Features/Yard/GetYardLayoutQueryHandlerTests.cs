namespace Depot.CleanArchitecture.Application.UnitTests.Features.Yard;

using Depot.CleanArchitecture.Application.Features.Yard.GetYardLayout;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GetYardLayoutQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_When20ftContainerOccupiesSlot_CountsCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var physicalBlock = YardBlock.CreatePhysical(
            _tenantId, "A", "Block A - Bãi Cont Khô", maxBay: 41, maxRow: 6, maxTier: 4, hasReeferPower: false);
        context.YardBlocks.Add(physicalBlock);

        var slot = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 1, row: 1, tier: 1);
        context.YardSlots.Add(slot);

        var cont20ft = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        context.Containers.Add(cont20ft);

        slot.Occupy(cont20ft.Id, cont20ft.Size);

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont20ft.Id,
            cont20ft.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont20ft.Grade,
            null);
        visit.AssignSlot(slot.Id, physicalBlock.Id);
        context.ContainerVisits.Add(visit);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("A"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.Code.Should().Be("A");
        blockDto.TotalContainers.Should().Be(1);
        blockDto.TierStats.Should().ContainSingle(t => t.Tier == 1 && t.ContainerCount == 1);
    }

    [Fact]
    public async Task HandleAsync_When40ftContainerOccupiesEvenBay_CountsDistinctContainerOnce()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var physicalBlock = YardBlock.CreatePhysical(
            _tenantId, "A", "Block A - Bãi Cont Khô", maxBay: 41, maxRow: 6, maxTier: 4, hasReeferPower: false);
        context.YardBlocks.Add(physicalBlock);

        var slotOdd5 = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 5, row: 1, tier: 1);
        var slotEven6 = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 6, row: 1, tier: 1);
        var slotOdd7 = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 7, row: 1, tier: 1);
        context.YardSlots.AddRange(slotOdd5, slotEven6, slotOdd7);

        var cont40ft = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU4000000").Value!,
            "ONE",
            ContainerType.Dry,
            "42G1",
            40,
            3800m,
            32500m,
            ContainerGrade.GradeA).Value!;
        context.Containers.Add(cont40ft);

        // Giả lập slot chẵn 6 được chiếm dụng bởi cont40ft
        slotEven6.Occupy(cont40ft.Id, cont40ft.Size);

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont40ft.Id,
            cont40ft.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont40ft.Grade,
            null);
        visit.AssignSlot(slotEven6.Id, physicalBlock.Id);
        context.ContainerVisits.Add(visit);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("A"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.Code.Should().Be("A");
        blockDto.TotalContainers.Should().Be(1, "Container 40ft chỉ được tính đúng 1 lần");
        blockDto.TierStats.Should().ContainSingle(t => t.Tier == 1 && t.ContainerCount == 1);
    }

    [Fact]
    public async Task HandleAsync_WhenPhysicalBlockHasMultipleTiers_CalculatesTierStatsCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var physicalBlock = YardBlock.CreatePhysical(
            _tenantId, "B", "Block B - Multi-Tier", maxBay: 41, maxRow: 6, maxTier: 4, hasReeferPower: true);
        context.YardBlocks.Add(physicalBlock);

        var slotTier1 = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 1, row: 1, tier: 1);
        var slotTier2 = YardSlot.Create(_tenantId, physicalBlock.Id, bay: 1, row: 1, tier: 2);
        context.YardSlots.AddRange(slotTier1, slotTier2);

        var cont1 = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Reefer,
            "22R1",
            20,
            2400m,
            30480m,
            ContainerGrade.GradeA).Value!;

        var cont2 = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU4000000").Value!,
            "ONE",
            ContainerType.Reefer,
            "22R1",
            20,
            2400m,
            30480m,
            ContainerGrade.GradeB).Value!;

        context.Containers.AddRange(cont1, cont2);

        slotTier1.Occupy(cont1.Id, cont1.Size);
        slotTier2.Occupy(cont2.Id, cont2.Size);

        var vehicle = VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!;
        var visit1 = ContainerVisit.CreateGateIn(_tenantId, cont1.Id, cont1.LineOperator, vehicle, cont1.Grade, null);
        visit1.AssignSlot(slotTier1.Id, physicalBlock.Id);

        var visit2 = ContainerVisit.CreateGateIn(_tenantId, cont2.Id, cont2.LineOperator, vehicle, cont2.Grade, null);
        visit2.AssignSlot(slotTier2.Id, physicalBlock.Id);

        context.ContainerVisits.AddRange(visit1, visit2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("B"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.TotalContainers.Should().Be(2);
        blockDto.TierStats.Should().HaveCount(4);
        blockDto.TierStats.First(t => t.Tier == 1).ContainerCount.Should().Be(1);
        blockDto.TierStats.First(t => t.Tier == 2).ContainerCount.Should().Be(1);
        blockDto.TierStats.First(t => t.Tier == 3).ContainerCount.Should().Be(0);
        blockDto.TierStats.First(t => t.Tier == 4).ContainerCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WhenBlockIsEmpty_ReturnsZeroTotalContainersAndZeroTiers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var emptyBlock = YardBlock.CreatePhysical(
            _tenantId, "C", "Block C - Bãi Trống", maxBay: 41, maxRow: 6, maxTier: 3, hasReeferPower: false);
        context.YardBlocks.Add(emptyBlock);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("C"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.TotalContainers.Should().Be(0);
        blockDto.TierStats.Should().HaveCount(3);
        blockDto.TierStats.Should().AllSatisfy(t => t.ContainerCount.Should().Be(0));
    }

    [Fact]
    public async Task HandleAsync_WhenBlockIsVirtual_ReturnsVirtualContainersWithCorrectDates()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", maxCapacity: 20);
        context.YardBlocks.Add(virtualBlock);

        var damagedCont = Container.Create(
            _tenantId,
            ContainerNumber.Create("EMCU8717530").Value!,
            "EMC",
            ContainerType.Dry,
            "42G1",
            40,
            3800m,
            32500m,
            ContainerGrade.GradeD_Damaged).Value!;
        context.Containers.Add(damagedCont);

        var gateInTime = DateTime.UtcNow.AddDays(-3);
        var moveTime = DateTime.UtcNow.AddDays(-1);

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            damagedCont.Id,
            damagedCont.LineOperator,
            VehicleInfo.Create("60C-341.29", "60R-789.01", "Lê Hoàng Nam", "0934567890").Value!,
            damagedCont.Grade,
            "Rách vách tôn");

        // Gán virtual block
        visit.AssignVirtualBlock(virtualBlock.Id);

        // Thêm movement event vào visit
        var moveEvent = MovementEvent.Create(
            _tenantId,
            visit.Id,
            "MOVE_TO_VIRTUAL",
            fromSlotId: Guid.NewGuid(),
            toSlotId: null,
            actorId: "actor-1",
            reason: "Chuyển sửa chữa");

        // Set thời gian cụ thể
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!
            .SetValue(moveEvent, moveTime);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit, gateInTime);

        context.MovementEvents.Add(moveEvent);
        context.ContainerVisits.Add(visit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("MR"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.IsVirtual.Should().BeTrue();
        blockDto.MaxCapacity.Should().Be(20);
        blockDto.TotalContainers.Should().Be(1);
        blockDto.VirtualContainers.Should().HaveCount(1);

        var item = blockDto.VirtualContainers[0];
        item.ContainerNumber.Should().Be("EMCU8717530");
        item.GateInDate.Should().BeCloseTo(gateInTime, TimeSpan.FromSeconds(1));
        item.EnteredWorkshopDate.Should().NotBeNull();
        item.EnteredWorkshopDate!.Value.Should().BeCloseTo(moveTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_WhenVirtualBlockIsEmpty_ReturnsZeroContainersAndEmptyList()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var emptyVirtualBlock = YardBlock.CreateVirtual(_tenantId, "WASH", "Khu Vực Rửa", maxCapacity: 10);
        context.YardBlocks.Add(emptyVirtualBlock);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("WASH"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.IsVirtual.Should().BeTrue();
        blockDto.MaxCapacity.Should().Be(10);
        blockDto.TotalContainers.Should().Be(0);
        blockDto.VirtualContainers.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenVirtualContainerHasNoMovementEvent_EnteredWorkshopDateIsNull()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", maxCapacity: 20);
        context.YardBlocks.Add(virtualBlock);

        var cont = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        context.Containers.Add(cont);

        var gateInTime = DateTime.UtcNow.AddDays(-2);
        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont.Id,
            cont.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont.Grade,
            null);
        visit.AssignVirtualBlock(virtualBlock.Id);

        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit, gateInTime);

        context.ContainerVisits.Add(visit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("MR"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.VirtualContainers.Should().HaveCount(1);

        var item = blockDto.VirtualContainers[0];
        item.EnteredWorkshopDate.Should().BeNull("Không tự suy diễn GateInDate làm EnteredWorkshopDate khi không có MovementEvent");
        item.GateInDate.Should().BeCloseTo(gateInTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_WhenContainerHasGateInMovementOnly_EnteredWorkshopDateIsNull()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", maxCapacity: 20);
        context.YardBlocks.Add(virtualBlock);

        var cont = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        context.Containers.Add(cont);

        var gateInTime = DateTime.UtcNow.AddDays(-2);
        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont.Id,
            cont.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont.Grade,
            null);
        visit.AssignVirtualBlock(virtualBlock.Id);

        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit, gateInTime);

        // Container có sự kiện GATE_IN (ToSlotId == null) nhưng CHƯA có sự kiện MOVE_TO_VIRTUAL
        var gateInEvent = MovementEvent.Create(
            _tenantId,
            visit.Id,
            "GATE_IN",
            fromSlotId: null,
            toSlotId: null,
            actorId: "Cổng C",
            reason: "Tiếp nhận cont");
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!
            .SetValue(gateInEvent, gateInTime);

        context.MovementEvents.Add(gateInEvent);
        context.ContainerVisits.Add(visit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("MR"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        var item = blockDto.VirtualContainers.Should().ContainSingle().Subject;
        item.EnteredWorkshopDate.Should().BeNull("Sự kiện GATE_IN không được nhận nhầm làm ngày vào xưởng");
        item.GateInDate.Should().BeCloseTo(gateInTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_WhenContainerRelocatedMultipleTimesBetweenYardAndVirtual_ReturnsLatestMoveToVirtualDate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var physicalBlock = YardBlock.CreatePhysical(_tenantId, "A", "Block A", 41, 6, 4);
        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", 20);
        context.YardBlocks.AddRange(physicalBlock, virtualBlock);

        var slot = YardSlot.Create(_tenantId, physicalBlock.Id, 1, 1, 1);
        context.YardSlots.Add(slot);

        var cont = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeD_Damaged).Value!;
        context.Containers.Add(cont);

        var t0 = DateTime.UtcNow.AddDays(-5); // GateIn
        var t1 = DateTime.UtcNow.AddDays(-4); // Vào xưởng lần 1
        var t2 = DateTime.UtcNow.AddDays(-2); // Về bãi sửa xong tạm thời
        var t3 = DateTime.UtcNow.AddDays(-1); // Vào xưởng lần 2

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont.Id,
            cont.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont.Grade,
            null);
        visit.AssignVirtualBlock(virtualBlock.Id); // Hiện tại đang ở MR

        var evGateIn = MovementEvent.Create(_tenantId, visit.Id, "GATE_IN", null, null, "Cổng");
        var evMove1 = MovementEvent.Create(_tenantId, visit.Id, "MOVE_TO_VIRTUAL", null, null, "Xe Kéo");
        var evMoveYard = MovementEvent.Create(_tenantId, visit.Id, "RELOCATE_YARD", null, slot.Id, "Xe Nâng");
        var evMove2 = MovementEvent.Create(_tenantId, visit.Id, "MOVE_TO_VIRTUAL", slot.Id, null, "Xe Kéo");

        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evGateIn, t0);
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evMove1, t1);
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evMoveYard, t2);
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evMove2, t3);

        context.MovementEvents.AddRange(evGateIn, evMove1, evMoveYard, evMove2);
        context.ContainerVisits.Add(visit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("MR"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Blocks.Single().VirtualContainers.Single();
        item.EnteredWorkshopDate.Should().NotBeNull();
        item.EnteredWorkshopDate!.Value.Should().BeCloseTo(t3, TimeSpan.FromSeconds(1), "Phải lấy mốc thời gian MOVE_TO_VIRTUAL gần nhất");
    }

    [Fact]
    public async Task HandleAsync_WhenContainerRelocatedFromMRToWASH_ReturnsLatestMoveToWashDate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var blockMR = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", 20);
        var blockWASH = YardBlock.CreateVirtual(_tenantId, "WASH", "Khu Vực Rửa", 15);
        context.YardBlocks.AddRange(blockMR, blockWASH);

        var cont = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeB).Value!;
        context.Containers.Add(cont);

        var tMR = DateTime.UtcNow.AddDays(-3);
        var tWASH = DateTime.UtcNow.AddDays(-1);

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont.Id,
            cont.LineOperator,
            VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!,
            cont.Grade,
            null);
        visit.AssignVirtualBlock(blockWASH.Id); // Đang ở WASH

        var evMoveMR = MovementEvent.Create(_tenantId, visit.Id, "MOVE_TO_VIRTUAL", null, null, "Xe Kéo");
        var evMoveWASH = MovementEvent.Create(_tenantId, visit.Id, "MOVE_TO_VIRTUAL", null, null, "Xe Kéo");

        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evMoveMR, tMR);
        typeof(MovementEvent).GetProperty(nameof(MovementEvent.OccurredAt))!.SetValue(evMoveWASH, tWASH);

        context.MovementEvents.AddRange(evMoveMR, evMoveWASH);
        context.ContainerVisits.Add(visit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("WASH"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Blocks.Single().VirtualContainers.Single();
        item.EnteredWorkshopDate.Should().NotBeNull();
        item.EnteredWorkshopDate!.Value.Should().BeCloseTo(tWASH, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_GradeFormatting_IsConsistentAcrossPhysicalSlotsAndVirtualContainers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var physicalBlock = YardBlock.CreatePhysical(_tenantId, "A", "Block A", 41, 6, 4);
        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xưởng M&R", 20);
        context.YardBlocks.AddRange(physicalBlock, virtualBlock);

        var slot = YardSlot.Create(_tenantId, physicalBlock.Id, 1, 1, 1);
        context.YardSlots.Add(slot);

        var contPhysical = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;

        var contVirtual = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU4000000").Value!,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeD_Damaged).Value!;

        context.Containers.AddRange(contPhysical, contVirtual);
        slot.Occupy(contPhysical.Id, contPhysical.Size);

        var vehicle = VehicleInfo.Create("51D-123.45", "51R-678.90", "Tài xế A", "0901234567").Value!;
        var visitPhysical = ContainerVisit.CreateGateIn(_tenantId, contPhysical.Id, "ONE", vehicle, contPhysical.Grade, null);
        visitPhysical.AssignSlot(slot.Id, physicalBlock.Id);

        var visitVirtual = ContainerVisit.CreateGateIn(_tenantId, contVirtual.Id, "ONE", vehicle, contVirtual.Grade, "Hư hỏng vách");
        visitVirtual.AssignVirtualBlock(virtualBlock.Id);

        context.ContainerVisits.AddRange(visitPhysical, visitVirtual);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery(null), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var physicalSlotDto = result.Value!.Slots.Single(s => s.Id == slot.Id);
        physicalSlotDto.Grade.Should().Be("Grade A", "Không bị lặp chuỗi 'Grade GradeA'");

        var virtualContainerDto = result.Value.Blocks.Single(b => b.Code == "MR").VirtualContainers.Single();
        virtualContainerDto.Grade.Should().Be("Grade D", "Không bị lặp chuỗi 'Grade GradeD_Damaged'");
    }

    [Fact]
    public async Task HandleAsync_WhenContainerGatedOut_DoesNotCountInTotalContainers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var block = YardBlock.CreatePhysical(_tenantId, "A", "Block A", maxBay: 41, maxRow: 6, maxTier: 4);
        context.YardBlocks.Add(block);

        var slot = YardSlot.Create(_tenantId, block.Id, bay: 1, row: 1, tier: 1);
        context.YardSlots.Add(slot);

        var cont = Container.Create(
            _tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        context.Containers.Add(cont);

        slot.Occupy(cont.Id, cont.Size);

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            cont.Id,
            cont.LineOperator,
            VehicleInfo.Create("51C-882.34", "51R-123.45", "Tài xế", "0908123456").Value!,
            cont.Grade,
            null);
        visit.GateOut(VehicleInfo.Create("51C-998.88", "51R-888.99", "Tài xế out", "0938889999").Value!);
        context.ContainerVisits.Add(visit);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardLayoutQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetYardLayoutQuery("A"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var blockDto = result.Value!.Blocks.Should().ContainSingle().Subject;
        blockDto.TotalContainers.Should().Be(0, "Container đã GateOut không được tính vào tồn bãi");
    }
}
