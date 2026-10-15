namespace Depot.CleanArchitecture.Application.UnitTests.Features.Yard;

using Depot.CleanArchitecture.Application.Features.Yard.PlaceContainer;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class PlaceContainerCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Success_And_Record_MovementEvent_When_Valid_Placement()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        dbContext.ContainerVisits.Add(visit);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        dbContext.YardBlocks.Add(block);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, block.Id, 1, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        visit.Status.Should().Be(ContainerVisitStatus.StackedInYard);
        visit.CurrentSlotId.Should().NotBeNull();
        visit.CurrentBlockId.Should().Be(block.Id);

        var slot = await dbContext.YardSlots.SingleAsync(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.IsOccupied.Should().BeTrue();
        slot.CurrentContainerId.Should().Be(container.Id);
        slot.OccupiedContainerSize.Should().Be(20);

        var movement = await dbContext.MovementEvents.SingleAsync(m => m.VisitId == visit.Id);
        movement.EventType.Should().Be("PLACE_YARD");
        movement.FromSlotId.Should().BeNull();
        movement.ToSlotId.Should().Be(slot.Id);
        movement.ActorId.Should().Be("YARD_DISPATCHER");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Container_Is_45ft()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "45G1",
            45,
            4200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        dbContext.ContainerVisits.Add(visit);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        dbContext.YardBlocks.Add(block);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, block.Id, 1, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.UnsupportedContainerSize");
        result.Error.Type.Should().Be(ErrorType.Validation);

        // Invariant: Visit remains GatedIn
        visit.Status.Should().Be(ContainerVisitStatus.GatedIn);
        visit.CurrentSlotId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Conflict_When_Visit_Is_Not_GatedIn()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        visit.GateOut(VehicleInfo.Create("51C-99999", "51R-99999", "Out Driver", "0909999999").Value!); // Status: GatedOut
        dbContext.ContainerVisits.Add(visit);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        dbContext.YardBlocks.Add(block);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, block.Id, 1, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Visit.InvalidStatus");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Conflict_When_Visit_Already_Has_Slot()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        var existingSlot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        existingSlot.Occupy(container.Id, 20);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        visit.AssignSlot(existingSlot.Id, block.Id); // Already placed

        dbContext.YardBlocks.Add(block);
        dbContext.YardSlots.Add(existingSlot);
        dbContext.ContainerVisits.Add(visit);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, block.Id, 3, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Visit.InvalidStatus");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Conflict_When_Slot_Is_Already_Occupied()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var cont1 = Container.Create(tenantId, ContainerNumber.Create("CSQU3054383").Value!, "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont2 = Container.Create(tenantId, ContainerNumber.Create("CSQU4000000").Value!, "ONE", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        dbContext.Containers.AddRange(cont1, cont2);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        var occupiedSlot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        occupiedSlot.Occupy(cont1.Id, 20);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit2 = ContainerVisit.CreateGateIn(tenantId, cont2.Id, "ONE", vehicle, ContainerGrade.GradeA, null);

        dbContext.YardBlocks.Add(block);
        dbContext.YardSlots.Add(occupiedSlot);
        dbContext.ContainerVisits.Add(visit2);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit2.Id, block.Id, 1, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.SlotOccupied");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Tier_Has_No_Bottom_Support()
    {
        // Arrange: Đặt ở Tier 2 khi Tier 1 chưa có container
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        dbContext.ContainerVisits.Add(visit);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        dbContext.YardBlocks.Add(block);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, block.Id, 1, 1, 2);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.TierMissingBottomSupport");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_EvenOddBayCollision()
    {
        // Arrange: Bay 1 (20ft) đã có cont -> Thử hạ cont 40ft vào Bay 2 (chồng lấn Bay 1 và Bay 3)
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var cont20 = Container.Create(tenantId, ContainerNumber.Create("CSQU3054383").Value!, "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        var cont40 = Container.Create(tenantId, ContainerNumber.Create("CSQU4000000").Value!, "ONE", ContainerType.Dry, "42G1", 40, 3800m, 32500m, ContainerGrade.GradeA).Value!;
        dbContext.Containers.AddRange(cont20, cont40);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5);
        var slotBay1 = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        slotBay1.Occupy(cont20.Id, 20);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit40 = ContainerVisit.CreateGateIn(tenantId, cont40.Id, "ONE", vehicle, ContainerGrade.GradeA, null);

        dbContext.YardBlocks.Add(block);
        dbContext.YardSlots.Add(slotBay1);
        dbContext.ContainerVisits.Add(visit40);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit40.Id, block.Id, 2, 1, 1); // 40ft into Bay 2

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Yard.EvenOddBayCollision");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Block_Is_Virtual()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "MAEU",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "MAEU", vehicle, ContainerGrade.GradeA, null);
        dbContext.ContainerVisits.Add(visit);

        var virtualBlock = YardBlock.CreateVirtual(tenantId, "MR", "Xưởng sửa chữa", 20);
        dbContext.YardBlocks.Add(virtualBlock);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PlaceContainerCommandHandler(dbContext);
        var command = new PlaceContainerCommand(tenantId, visit.Id, virtualBlock.Id, 1, 1, 1);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.VirtualNotSupported");
    }
}
