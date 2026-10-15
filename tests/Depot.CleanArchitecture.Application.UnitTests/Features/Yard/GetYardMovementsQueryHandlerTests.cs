namespace Depot.CleanArchitecture.Application.UnitTests.Features.Yard;

using Depot.CleanArchitecture.Application.Features.Yard.GetYardMovements;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GetYardMovementsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Movements_When_Data_Exists()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var container = Container.Create(
            tenantId,
            ContainerNumber.Create("CSQU3054383").Value!,
            "ONE",
            ContainerType.Dry,
            "42G1",
            40,
            3800m,
            32500m,
            ContainerGrade.GradeA).Value!;
        dbContext.Containers.Add(container);

        var vehicle = VehicleInfo.Create("51C-99999", "51R-88888", "Tài Xế A", "0901234567").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, "ONE", vehicle, ContainerGrade.GradeA, null);
        dbContext.ContainerVisits.Add(visit);

        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 10, 6, 5);
        dbContext.YardBlocks.Add(block);

        var slot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        dbContext.YardSlots.Add(slot);

        var movement = MovementEvent.Create(tenantId, visit.Id, "PLACE_YARD", null, slot.Id, "CRANE-01", "Hạ bãi ban đầu");
        dbContext.MovementEvents.Add(movement);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardMovementsQueryHandler(dbContext);
        var query = new GetYardMovementsQuery(ContainerNumber: "CSQU3054383");

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Activity.Should().Be("Hạ bãi");
        result.Value![0].ToLocation.Should().Be("A-A01-01");
    }
}
