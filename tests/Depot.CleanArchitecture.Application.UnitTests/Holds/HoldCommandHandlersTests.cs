namespace Depot.CleanArchitecture.Application.UnitTests.Holds;

using Depot.CleanArchitecture.Application.Features.Gate.GateOut;
using Depot.CleanArchitecture.Application.Features.Holds.ApplyHold;
using Depot.CleanArchitecture.Application.Features.Holds.ReleaseHold;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class HoldCommandHandlersTests
{
    [Fact]
    public async Task GateOut_WhenContainerHasActiveHold_ShouldFailWithRuleError()
    {
        var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var contNum = ContainerNumber.Create("CSQU3054383").Value!;
        var container = Container.Create(
            tenantId,
            contNum,
            "ONE",
            ContainerType.Dry,
            "22G1",
            20,
            2200m,
            30480m,
            ContainerGrade.GradeA).Value!;
        db.Containers.Add(container);

        var visit = ContainerVisit.CreateGateIn(
            tenantId,
            container.Id,
            "ONE",
            VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!,
            ContainerGrade.GradeA,
            null);
        visit.ApplyHold("CUSTOMS_HOLD", "Soi chieu", "HQ");
        db.ContainerVisits.Add(visit);

        var doOrder = DeliveryOrder.Create(
            tenantId,
            "DO-001",
            "ONE",
            "Cty XNK A",
            "0312345678",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            "VESSEL 1",
            "VOY 1");
        doOrder.AddItem(20, ContainerType.Dry, ContainerGrade.GradeA, 1);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync();

        var handler = new GateOutCommandHandler(db);
        var command = new GateOutCommand(tenantId, "CSQU3054383", "DO-001", "51C-1", "51R-1", "A", "090");

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.ContainerHasActiveHold");
    }

    [Fact]
    public async Task ApplyHold_And_ReleaseHold_Lifecycle_ShouldSucceed()
    {
        var db = TestDbContextFactory.Create();
        var visit = ContainerVisit.CreateGateIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ONE",
            VehicleInfo.Create("51C-1", "51R-1", "A", "090").Value!,
            ContainerGrade.GradeA,
            null);
        db.ContainerVisits.Add(visit);
        await db.SaveChangesAsync();

        var applyHandler = new ApplyHoldCommandHandler(db);
        var applyCommand = new ApplyHoldCommand(visit.Id, "DOCUMENT_HOLD", "Thieu chung tu giay to", "NV-CHUNGTU");
        var applyResult = await applyHandler.HandleAsync(applyCommand);

        applyResult.IsSuccess.Should().BeTrue();
        applyResult.Value!.HoldId.Should().NotBeEmpty();

        var releaseHandler = new ReleaseHoldCommandHandler(db);
        var releaseCommand = new ReleaseHoldCommand(visit.Id, applyResult.Value.HoldId, "Da bo sung xong chung tu", "NV-CHUNGTU");
        var releaseResult = await releaseHandler.HandleAsync(releaseCommand);

        releaseResult.IsSuccess.Should().BeTrue();
    }
}
