namespace Depot.CleanArchitecture.Application.UnitTests.Features.Yard;

using Depot.CleanArchitecture.Application.Features.Yard.GetPendingPlacements;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GetPendingPlacementsQueryHandlerTests
{
    private static Container CreateContainer(
        Guid tenantId,
        string numberStr,
        string lineOperator = "ONE",
        ContainerType type = ContainerType.Dry,
        string isoCode = "22G1",
        int size = 20,
        ContainerGrade grade = ContainerGrade.GradeA)
    {
        var (tare, maxGross, _) = Container.GetDefaultSpecs(size, type);
        return Container.Create(
            tenantId,
            ContainerNumber.Create(numberStr).Value!,
            lineOperator,
            type,
            isoCode,
            size,
            tare,
            maxGross,
            grade).Value!;
    }

    private static VehicleInfo CreateVehicle(
        string tractor = "51C-12345",
        string trailer = "51R-67890",
        string driver = "Nguyen Van A",
        string? phone = "0901234567")
    {
        return VehicleInfo.Create(tractor, trailer, driver, phone).Value!;
    }

    [Fact]
    public async Task HandleAsync_WhenNoPendingVisits_ShouldReturnEmptyList()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenTwoGatedInVisitsWithoutSlot_ShouldReturnBoth()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var cont1 = CreateContainer(tenantId, "CSQU3054383");
        var cont2 = CreateContainer(tenantId, "TEMU1234565");
        dbContext.Containers.AddRange(cont1, cont2);

        var visit1 = ContainerVisit.CreateGateIn(tenantId, cont1.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        var visit2 = ContainerVisit.CreateGateIn(tenantId, cont2.Id, "ONE", CreateVehicle(), ContainerGrade.GradeB, null);
        dbContext.ContainerVisits.AddRange(visit1, visit2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(x => x.VisitId).Should().Contain([visit1.Id, visit2.Id]);
    }

    [Fact]
    public async Task HandleAsync_WhenVisitIsStackedInYard_ShouldBeExcluded()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var contPending = CreateContainer(tenantId, "CSQU3054383");
        var contPlaced = CreateContainer(tenantId, "TEMU1234565");
        dbContext.Containers.AddRange(contPending, contPlaced);

        var visitPending = ContainerVisit.CreateGateIn(tenantId, contPending.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        var visitPlaced = ContainerVisit.CreateGateIn(tenantId, contPlaced.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        visitPlaced.AssignSlot(Guid.NewGuid(), Guid.NewGuid()); // StackedInYard

        dbContext.ContainerVisits.AddRange(visitPending, visitPlaced);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().VisitId.Should().Be(visitPending.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenVisitIsAllocatedOrGatedOutOrCompleted_ShouldBeExcluded()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var c1 = CreateContainer(tenantId, "CSQU3054383");
        var c2 = CreateContainer(tenantId, "TEMU1234565");
        var c3 = CreateContainer(tenantId, "APZU4812090");
        dbContext.Containers.AddRange(c1, c2, c3);

        var v1 = ContainerVisit.CreateGateIn(tenantId, c1.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        v1.AllocateToOrder(Guid.NewGuid()); // Allocated

        var v2 = ContainerVisit.CreateGateIn(tenantId, c2.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        v2.GateOut(CreateVehicle()); // GatedOut

        var v3 = ContainerVisit.CreateGateIn(tenantId, c3.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        v3.Complete(); // Completed

        dbContext.ContainerVisits.AddRange(v1, v2, v3);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenVisitHasCurrentBlockOrSlot_ShouldBeExcluded()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var c1 = CreateContainer(tenantId, "CSQU3054383");
        var c2 = CreateContainer(tenantId, "TEMU1234565");
        dbContext.Containers.AddRange(c1, c2);

        var v1 = ContainerVisit.CreateGateIn(tenantId, c1.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.CurrentSlotId))!.SetValue(v1, Guid.NewGuid());

        var v2 = ContainerVisit.CreateGateIn(tenantId, c2.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.CurrentBlockId))!.SetValue(v2, Guid.NewGuid());

        dbContext.ContainerVisits.AddRange(v1, v2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderByGateInDateAscending()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var cOld = CreateContainer(tenantId, "CSQU3054383");
        var cMid = CreateContainer(tenantId, "TEMU1234565");
        var cNew = CreateContainer(tenantId, "APZU4812090");
        dbContext.Containers.AddRange(cOld, cMid, cNew);

        var baseTime = DateTime.UtcNow;
        var vOld = ContainerVisit.CreateGateIn(tenantId, cOld.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!.SetValue(vOld, baseTime.AddHours(-3));

        var vMid = ContainerVisit.CreateGateIn(tenantId, cMid.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!.SetValue(vMid, baseTime.AddHours(-2));

        var vNew = ContainerVisit.CreateGateIn(tenantId, cNew.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!.SetValue(vNew, baseTime.AddHours(-1));

        // Add in random order
        dbContext.ContainerVisits.AddRange(vMid, vNew, vOld);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value!.Select(x => x.VisitId).Should().ContainInConsecutiveOrder(vOld.Id, vMid.Id, vNew.Id);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateWaitingMinutesAccuratelyAndNonNegative()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var cPast = CreateContainer(tenantId, "CSQU3054383");
        var cFuture = CreateContainer(tenantId, "TEMU1234565");
        dbContext.Containers.AddRange(cPast, cFuture);

        var now = DateTime.UtcNow;
        var vPast = ContainerVisit.CreateGateIn(tenantId, cPast.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!.SetValue(vPast, now.AddMinutes(-45));

        // Simulating minor clock skew
        var vFuture = ContainerVisit.CreateGateIn(tenantId, cFuture.Id, "ONE", CreateVehicle(), ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!.SetValue(vFuture, now.AddMinutes(5));

        dbContext.ContainerVisits.AddRange(vPast, vFuture);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var pastDto = result.Value!.Single(x => x.VisitId == vPast.Id);
        pastDto.WaitingMinutes.Should().BeInRange(44, 46);

        var futureDto = result.Value!.Single(x => x.VisitId == vFuture.Id);
        futureDto.WaitingMinutes.Should().Be(0, "Clock skew in future must clamp to 0 instead of negative");
    }

    [Fact]
    public async Task HandleAsync_ShouldMapDtoPropertiesCorrectly()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = CreateContainer(
            tenantId,
            "CSQU3054383",
            lineOperator: "ONE",
            type: ContainerType.Reefer,
            isoCode: "45R1",
            size: 40,
            grade: ContainerGrade.GradeA);
        dbContext.Containers.Add(container);

        var vehicle = CreateVehicle(
            tractor: "51C-99999",
            trailer: "51R-88888",
            driver: "Tran Van Tai",
            phone: "0912345678");

        var visit = ContainerVisit.CreateGateIn(
            tenantId,
            container.Id,
            "ONE",
            vehicle,
            ContainerGrade.GradeA,
            "Cua cont bi tray xuoc nhe");
        dbContext.ContainerVisits.Add(visit);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPendingPlacementsQueryHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(new GetPendingPlacementsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().ContainSingle().Subject;

        dto.VisitId.Should().Be(visit.Id);
        dto.ContainerId.Should().Be(container.Id);
        dto.ContainerNumber.Should().Be("CSQU3054383");
        dto.LineOperator.Should().Be("ONE");
        dto.Size.Should().Be(40);
        dto.Type.Should().Be("Reefer");
        dto.IsoCode.Should().Be("45R1");
        dto.Grade.Should().Be("GradeA");
        dto.GradeDescription.Should().Be("Grade A (Đạt chuẩn đóng hàng xuất khẩu)");
        dto.IsReefer.Should().BeTrue();
        dto.GateInDate.Should().BeCloseTo(visit.GateInDate, TimeSpan.FromSeconds(2));
        dto.WaitingMinutes.Should().BeGreaterThanOrEqualTo(0);
        dto.TractorNo.Should().Be("51C-99999");
        dto.TrailerNo.Should().Be("51R-88888");
        dto.DriverName.Should().Be("Tran Van Tai");
        dto.DriverPhone.Should().Be("0912345678");
        dto.DamageNotes.Should().Be("Cua cont bi tray xuoc nhe");
    }
}
