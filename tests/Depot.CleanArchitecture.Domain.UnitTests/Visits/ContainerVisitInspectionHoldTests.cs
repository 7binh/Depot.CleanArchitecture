namespace Depot.CleanArchitecture.Domain.UnitTests.Visits;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerVisitInspectionHoldTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _containerId = Guid.NewGuid();

    [Fact]
    public void ConditionSurvey_Create_WithValidData_ShouldSucceed()
    {
        var visitId = Guid.NewGuid();
        var surveyResult = ConditionSurvey.Create(
            _tenantId,
            visitId,
            ContainerGrade.GradeB,
            condition: "GOOD",
            damageCodes: ["RUST_MINOR"],
            notes: "Vết rỉ nhẹ trên nóc",
            photoRefs: "https://photos.snp.vn/img1.jpg",
            inspectedBy: "NV-GIAMDINH-01");

        surveyResult.IsSuccess.Should().BeTrue();
        var survey = surveyResult.Value!;
        survey.TenantId.Should().Be(_tenantId);
        survey.VisitId.Should().Be(visitId);
        survey.Grade.Should().Be(ContainerGrade.GradeB);
        survey.Condition.Should().Be("GOOD");
        survey.DamageCodes.Should().ContainSingle().Which.Should().Be("RUST_MINOR");
        survey.InspectedBy.Should().Be("NV-GIAMDINH-01");
    }

    [Fact]
    public void StayHold_Lifecycle_CreateAndRelease_ShouldUpdateStatus()
    {
        var visitId = Guid.NewGuid();
        var holdResult = StayHold.Create(
            _tenantId,
            visitId,
            holdCode: "CUSTOMS_HOLD",
            reason: "Chờ kiểm hóa hải quan",
            heldBy: "HQ-CATLAI");

        holdResult.IsSuccess.Should().BeTrue();
        var hold = holdResult.Value!;
        hold.IsActive.Should().BeTrue();
        hold.HoldCode.Should().Be("CUSTOMS_HOLD");

        var releaseResult = hold.Release("Hải quan đã kiểm tra xong", "HQ-CATLAI");
        releaseResult.IsSuccess.Should().BeTrue();
        hold.IsActive.Should().BeFalse();
        hold.ReleasedAt.Should().NotBeNull();
        hold.ReleaseReason.Should().Be("Hải quan đã kiểm tra xong");
    }

    [Fact]
    public void ContainerVisit_AddSurvey_WithGradeD_ShouldAutoTriggerDamageHold()
    {
        var vehicle = VehicleInfo.Create("51C-111.11", "51R-222.22", "Nguyen Van A").Value!;
        var visit = ContainerVisit.CreateGateIn(
            _tenantId,
            _containerId,
            lineOperator: "ONE",
            vehicle,
            surveyGrade: ContainerGrade.GradeA,
            damageNotes: null);

        var survey = ConditionSurvey.Create(
            _tenantId,
            visit.Id,
            ContainerGrade.GradeD_Damaged,
            condition: "DAMAGED",
            damageCodes: ["HOLE", "BENT"],
            notes: "Thủng sàn và cong đà dọc",
            photoRefs: null,
            inspectedBy: "NV-GIAMDINH-02").Value!;

        visit.AddSurvey(survey);

        visit.Surveys.Should().ContainSingle();
        visit.InSurveyGrade.Should().Be(ContainerGrade.GradeD_Damaged);
        visit.Holds.Should().ContainSingle(h => h.HoldCode == "DAMAGE_HOLD" && h.IsActive);
    }
}
