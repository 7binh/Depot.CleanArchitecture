namespace Depot.CleanArchitecture.Application.UnitTests.Features.Gate;

using Depot.CleanArchitecture.Application.Features.Gate.GateIn;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GateInCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Success_And_Record_MovementEvent_When_Valid_GateIn()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var tenantId = Guid.NewGuid();
        var command = new GateInCommand(
            TenantId: tenantId,
            ContainerNumber: "TEMU1234565",
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeA,
            LineOperator: "MAEU",
            TractorNo: "51C-12345",
            TrailerNo: "51R-67890",
            DriverName: "Nguyen Van A",
            DriverPhone: "0901234567",
            DamageNotes: "Vo container sach dep");

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContainerNumber.Should().Be("TEMU1234565");
        result.Value.Status.Should().Be("GatedIn");

        dbContext.Containers.Should().HaveCount(1);
        dbContext.ContainerVisits.Should().HaveCount(1);
        dbContext.MovementEvents.Should().HaveCount(1);

        var visit = dbContext.ContainerVisits.First();
        visit.Status.Should().Be(ContainerVisitStatus.GatedIn);
        visit.CurrentBlockId.Should().BeNull();
        visit.CurrentSlotId.Should().BeNull();
        visit.InDamageNotes.Should().Be("Vo container sach dep");

        var movement = dbContext.MovementEvents.First();
        movement.EventType.Should().Be("GATE_IN");
        movement.VisitId.Should().Be(visit.Id);
        movement.FromSlotId.Should().BeNull();
        movement.ToSlotId.Should().BeNull();
        movement.ActorId.Should().Be("GATE_CLERK");
        movement.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_ContainerNumber_Invalid()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var command = new GateInCommand(
            TenantId: Guid.NewGuid(),
            ContainerNumber: "INVALID123",
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeA,
            LineOperator: "MAEU",
            TractorNo: "51C-12345",
            TrailerNo: "51R-67890",
            DriverName: "Nguyen Van A",
            DriverPhone: "0901234567",
            DamageNotes: null);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidLength");
        dbContext.Containers.Should().BeEmpty();
        dbContext.ContainerVisits.Should().BeEmpty();
        dbContext.MovementEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_ContainerNumber_Has_Invalid_CheckDigit()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var command = new GateInCommand(
            TenantId: Guid.NewGuid(),
            ContainerNumber: "TEMU1234560", // 0 is invalid check digit (correct is 5)
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeA,
            LineOperator: "MAEU",
            TractorNo: "51C-12345",
            TrailerNo: "51R-67890",
            DriverName: "Nguyen Van A",
            DriverPhone: "0901234567",
            DamageNotes: null);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidCheckDigit");
        dbContext.Containers.Should().BeEmpty();
        dbContext.ContainerVisits.Should().BeEmpty();
        dbContext.MovementEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Conflict_When_Container_Has_Active_Visit()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var tenantId = Guid.NewGuid();
        var command = new GateInCommand(
            TenantId: tenantId,
            ContainerNumber: "TEMU1234565",
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeA,
            LineOperator: "MAEU",
            TractorNo: "51C-12345",
            TrailerNo: "51R-67890",
            DriverName: "Nguyen Van A",
            DriverPhone: "0901234567",
            DamageNotes: null);

        // First Gate-In
        var firstResult = await handler.HandleAsync(command, TestContext.Current.CancellationToken);
        firstResult.IsSuccess.Should().BeTrue();

        // Act - Second Gate-In for the same active container
        var secondResult = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Visit.AlreadyActive");
        secondResult.Error.Type.Should().Be(ErrorType.Conflict);

        // Still exactly 1 visit and 1 movement event
        dbContext.ContainerVisits.Should().HaveCount(1);
        dbContext.MovementEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Reuse_Existing_Container_When_Past_Visit_Is_GatedOut()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var tenantId = Guid.NewGuid();
        var num = ContainerNumber.Create("TEMU1234565").Value!;
        var existingContainer = Container.Create(tenantId, num, "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(existingContainer);

        // Create completed/gated out past visit
        var pastVisit = ContainerVisit.CreateGateIn(
            tenantId,
            existingContainer.Id,
            "MAEU",
            VehicleInfo.Create("51C-00000", "51R-00000", "Old Driver", "0900000000").Value!,
            ContainerGrade.GradeA,
            null);
        pastVisit.GateOut(VehicleInfo.Create("51C-99999", "51R-99999", "Out Driver", "0909999999").Value!);
        dbContext.ContainerVisits.Add(pastVisit);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var command = new GateInCommand(
            TenantId: tenantId,
            ContainerNumber: "TEMU1234565",
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeB,
            LineOperator: "MAEU",
            TractorNo: "51C-12345",
            TrailerNo: "51R-67890",
            DriverName: "New Driver",
            DriverPhone: "0901234567",
            DamageNotes: "Mop nhe");

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        dbContext.Containers.Should().HaveCount(1); // Reused existing container
        dbContext.ContainerVisits.Should().HaveCount(2); // Past GatedOut visit + New GatedIn visit
        dbContext.MovementEvents.Should().HaveCount(1); // Exactly 1 new movement event
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_VehicleInfo_Invalid()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GateInCommandHandler(dbContext);
        var command = new GateInCommand(
            TenantId: Guid.NewGuid(),
            ContainerNumber: "TEMU1234565",
            Size: 20,
            Type: ContainerType.Dry,
            Grade: ContainerGrade.GradeA,
            LineOperator: "MAEU",
            TractorNo: "", // Empty tractor no
            TrailerNo: "51R-67890",
            DriverName: "Nguyen Van A",
            DriverPhone: "0901234567",
            DamageNotes: null);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Vehicle.InvalidLicense");
        dbContext.Containers.Should().BeEmpty();
        dbContext.ContainerVisits.Should().BeEmpty();
        dbContext.MovementEvents.Should().BeEmpty();
    }
}
