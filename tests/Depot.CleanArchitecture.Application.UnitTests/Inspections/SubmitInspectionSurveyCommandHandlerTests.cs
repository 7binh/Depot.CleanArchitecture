namespace Depot.CleanArchitecture.Application.UnitTests.Inspections;

using Depot.CleanArchitecture.Application.Features.Inspections.SubmitInspectionSurvey;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class SubmitInspectionSurveyCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithGradeD_ShouldSaveSurveyAndTriggerDamageHold()
    {
        var db = TestDbContextFactory.Create();
        var visit = ContainerVisit.CreateGateIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CMA",
            VehicleInfo.Create("51C-1", "51R-2", "Driver").Value!,
            ContainerGrade.GradeA,
            null);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var handler = new SubmitInspectionSurveyCommandHandler(db);
        var command = new SubmitInspectionSurveyCommand(
            visit.Id,
            ContainerGrade.GradeD_Damaged,
            "DAMAGED",
            ["HOLE"],
            "Thủng sàn",
            null,
            "Inspector01");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDamageHoldTriggered.Should().BeTrue();
        visit.Holds.Should().ContainSingle(h => h.HoldCode == "DAMAGE_HOLD");
    }
}
