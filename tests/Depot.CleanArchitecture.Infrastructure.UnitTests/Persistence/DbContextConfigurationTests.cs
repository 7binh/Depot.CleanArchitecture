namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Persistence;

using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class DbContextConfigurationTests
{
    [Fact]
    public async Task AppDbContext_CanPersistAndRetrieve_ConditionSurvey_StayHold_MovementEvent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantId = TenantInfo.DefaultDepotCatLaiId;
        var visitId = Guid.NewGuid();

        using (var context = new AppDbContext(options))
        {
            var survey = ConditionSurvey.Create(
                tenantId,
                visitId,
                ContainerGrade.GradeB,
                "GOOD",
                ["RUST"],
                "Vet ri nho",
                null,
                "Inspector01").Value!;

            var hold = StayHold.Create(
                tenantId,
                visitId,
                "DAMAGE_HOLD",
                "Hu hong vo",
                "Inspector01").Value!;

            var movement = MovementEvent.Create(
                tenantId,
                visitId,
                "RELOCATE_YARD",
                null,
                null,
                "Actor01",
                "Ly do di chuyen");

            context.ConditionSurveys.Add(survey);
            context.StayHolds.Add(hold);
            context.MovementEvents.Add(movement);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var context = new AppDbContext(options))
        {
            var surveys = await context.ConditionSurveys.ToListAsync(TestContext.Current.CancellationToken);
            var holds = await context.StayHolds.ToListAsync(TestContext.Current.CancellationToken);
            var movements = await context.MovementEvents.ToListAsync(TestContext.Current.CancellationToken);

            surveys.Should().ContainSingle();
            holds.Should().ContainSingle();
            movements.Should().ContainSingle();
        }
    }
}
