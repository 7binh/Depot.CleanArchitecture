namespace Depot.CleanArchitecture.Domain.UnitTests.Visits;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerVisitLifecycleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _containerId = Guid.NewGuid();

    [Fact]
    public void VehicleInfo_Create_WithValidData_ShouldSucceed()
    {
        var result = VehicleInfo.Create("51C-123.45", "51R-678.90", "Nguyen Van A", "0901234567");

        result.IsSuccess.Should().BeTrue();
        result.Value.TractorNo.Should().Be("51C-123.45");
        result.Value.TrailerNo.Should().Be("51R-678.90");
        result.Value.DriverName.Should().Be("Nguyen Van A");
        result.Value.DriverPhone.Should().Be("0901234567");
    }

    [Theory]
    [InlineData("", "51R-678.90", "Nguyen Van A")]
    [InlineData("51C-123.45", "", "Nguyen Van A")]
    [InlineData("   ", "51R-678.90", "Nguyen Van A")]
    public void VehicleInfo_Create_WithMissingLicense_ShouldFail(string tractor, string trailer, string driver)
    {
        var result = VehicleInfo.Create(tractor, trailer, driver);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Vehicle.InvalidLicense");
    }

    [Fact]
    public void VehicleInfo_Create_WithMissingDriver_ShouldFail()
    {
        var result = VehicleInfo.Create("51C-123.45", "51R-678.90", "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Vehicle.InvalidDriver");
    }

    [Fact]
    public void ContainerVisit_Lifecycle_StateTransitions_ShouldFollowSequence()
    {
        var inVehicle = VehicleInfo.Create("51C-111.11", "51R-222.22", "Tran Van In").Value;
        var outVehicle = VehicleInfo.Create("51C-333.33", "51R-444.44", "Le Van Out").Value;
        var blockId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var doId = Guid.NewGuid();

        // 1. Gate In
        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            _containerId,
            lineOperator: "cma-cgm",
            inVehicle,
            surveyGrade: ContainerGrade.GradeA,
            damageNotes: "San sach dep, khong mui");

        visit.Status.Should().Be(ContainerVisitStatus.GatedIn);
        visit.LineOperator.Should().Be("CMA-CGM");
        visit.InVehicle.Should().Be(inVehicle);
        visit.InSurveyGrade.Should().Be(ContainerGrade.GradeA);
        visit.GateInDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // 2. Stack In Yard
        visit.AssignSlot(slotId, blockId);
        visit.Status.Should().Be(ContainerVisitStatus.StackedInYard);
        visit.CurrentSlotId.Should().Be(slotId);
        visit.CurrentBlockId.Should().Be(blockId);

        // 3. Allocate to Order
        visit.AllocateToOrder(doId);
        visit.Status.Should().Be(ContainerVisitStatus.Allocated);
        visit.DeliveryOrderId.Should().Be(doId);

        // 4. Gate Out
        visit.GateOut(outVehicle);
        visit.Status.Should().Be(ContainerVisitStatus.GatedOut);
        visit.OutVehicle.Should().Be(outVehicle);
        visit.CurrentSlotId.Should().BeNull(); // Da giai phong slot
        visit.GateOutDate.Should().NotBeNull();

        // 5. Complete
        visit.Complete();
        visit.Status.Should().Be(ContainerVisitStatus.Completed);
    }

    [Fact]
    public void ContainerVisit_AssignVirtualBlock_ShouldSetBlockAndKeepSlotNull()
    {
        var vehicle = VehicleInfo.Create("51C-111.11", "51R-222.22", "Tran Van In").Value;
        var virtualBlockId = Guid.NewGuid();

        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            _containerId,
            lineOperator: "MSC",
            vehicle,
            surveyGrade: ContainerGrade.GradeD_Damaged,
            damageNotes: "Thung vach, chuyen xuong M&R");

        visit.AssignVirtualBlock(virtualBlockId);

        visit.Status.Should().Be(ContainerVisitStatus.StackedInYard);
        visit.CurrentBlockId.Should().Be(virtualBlockId);
        visit.CurrentSlotId.Should().BeNull();
    }
}
