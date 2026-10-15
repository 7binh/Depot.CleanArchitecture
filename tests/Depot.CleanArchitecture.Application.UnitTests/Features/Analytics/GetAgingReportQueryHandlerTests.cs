namespace Depot.CleanArchitecture.Application.UnitTests.Features.Analytics;

using Depot.CleanArchitecture.Application.Features.Analytics.GetAgingReport;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GetAgingReportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Correctly_Group_And_Count_Aging_Containers()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;

        // Container 1: MAEU vào bãi 5 ngày trước (<= 10 ngày)
        var visit1 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "MAEU", vehicle, ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit1, DateTime.UtcNow.AddDays(-5));

        // Container 2: MAEU vào bãi 15 ngày trước (> 10 ngày)
        var visit2 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "MAEU", vehicle, ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit2, DateTime.UtcNow.AddDays(-15));

        // Container 3: ONE vào bãi 3 ngày trước (<= 10 ngày)
        var visit3 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "ONE", vehicle, ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit3, DateTime.UtcNow.AddDays(-3));

        // Container 4: Đã GatedOut -> không được tính vào tồn bãi
        var visit4 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "MAEU", vehicle, ContainerGrade.GradeA, null);
        visit4.GateOut(vehicle);

        dbContext.ContainerVisits.AddRange(visit1, visit2, visit3, visit4);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAgingReportQueryHandler(dbContext);
        var query = new GetAgingReportQuery();

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var maeuReport = result.Value!.First(r => r.LineOperator == "MAEU");
        maeuReport.Count0To10Days.Should().Be(1);
        maeuReport.CountOver10Days.Should().Be(1);
        maeuReport.TotalInventory.Should().Be(2);

        var oneReport = result.Value.First(r => r.LineOperator == "ONE");
        oneReport.Count0To10Days.Should().Be(1);
        oneReport.CountOver10Days.Should().Be(0);
        oneReport.TotalInventory.Should().Be(1);
    }
}
