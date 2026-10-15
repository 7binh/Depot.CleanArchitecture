namespace Depot.CleanArchitecture.Application.UnitTests.Features.Analytics;

using Depot.CleanArchitecture.Application.Features.Analytics.GetDailyThroughputReport;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GetDailyThroughputReportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Correct_Throughput_Movements()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var vehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Driver", "0900000000").Value!;
        var today = DateTime.UtcNow;

        // Visit 1: GateIn hôm nay (hãng MAEU)
        var visit1 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "MAEU", vehicle, ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit1, today);

        // Visit 2: GateIn và GateOut hôm nay (hãng MAEU)
        var visit2 = ContainerVisit.CreateGateIn(tenantId, Guid.NewGuid(), "MAEU", vehicle, ContainerGrade.GradeA, null);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))!
            .SetValue(visit2, today);
        visit2.GateOut(vehicle);
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateOutDate))!
            .SetValue(visit2, today);

        dbContext.ContainerVisits.AddRange(visit1, visit2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetDailyThroughputReportQueryHandler(dbContext);
        var fromDate = DateOnly.FromDateTime(today.AddDays(-1));
        var toDate = DateOnly.FromDateTime(today.AddDays(1));
        var query = new GetDailyThroughputReportQuery(fromDate, toDate);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var todayReport = result.Value!.First(r => r.LineOperator == "MAEU");
        todayReport.GateInCount.Should().Be(2);
        todayReport.GateOutCount.Should().Be(1);
        todayReport.TotalMovements.Should().Be(3);
    }
}
