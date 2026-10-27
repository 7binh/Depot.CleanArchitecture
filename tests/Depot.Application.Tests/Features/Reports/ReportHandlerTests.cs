using Depot.Application.Features.Reports.Throughput;
using Depot.Application.Features.Reports.YardStock;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Reports;

public sealed class ReportHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // The test clock reads 2026-10-26 12:00 UTC.
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly _today = new(2026, 10, 26);

    private static DateTimeOffset At(int month, int day, int hour = 9)
    {
        return new DateTimeOffset(2026, month, day, hour, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Container 1 (operator CSQ) at tier 1 and container 2 (operator ABC) at tier 2 of the same row.</summary>
    private sealed record Scenario(AppDbContext DbContext, GateInYard Yard, Guid SecondId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }
    }

    private static async Task<Scenario> CreateScenarioAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var secondId = await GateInTestData.CreateSecondContainerAsync(dbContext, _ct);
        return new Scenario(dbContext, yard, secondId);
    }

    private static async Task GateInAsync(Scenario scenario, Guid containerId, int tier, DateTimeOffset at)
    {
        var yard = scenario.Yard;
        var result = await GateInTestData.CreateHandler(scenario.DbContext)
            .HandleAsync(GateInTestData.Command(containerId, yard.BlockId, yard.RowId, tier, at), _ct);
        Assert.True(result.IsSuccess);
    }

    private static async Task GateOutSecondAsync(Scenario scenario, DateTimeOffset at)
    {
        var abcId = await scenario.DbContext.LineOperators.Where(x => x.Code == "ABC").Select(x => x.Id).SingleAsync(_ct);
        var orderId = await GateOutTestData.CreateOrderAsync(scenario.DbContext, 1, _ct, operatorId: abcId, orderNo: "RO-ABC");
        var result = await GateOutTestData.CreateHandler(scenario.DbContext)
            .HandleAsync(GateOutTestData.Command(scenario.SecondId, orderId, at), _ct);
        Assert.True(result.IsSuccess);
    }

    private static GetThroughputHandler Throughput(AppDbContext dbContext, TimeProvider? clock = null)
    {
        return new GetThroughputHandler(dbContext, clock ?? ContainerTestData.Clock);
    }

    private static GetYardStockHandler YardStock(AppDbContext dbContext, TimeProvider? clock = null)
    {
        return new GetYardStockHandler(dbContext, clock ?? ContainerTestData.Clock);
    }

    [Fact]
    public async Task Throughput_CountsGateInsAndGateOuts_PerDayAndOperator()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 24));
        await GateInAsync(scenario, scenario.SecondId, 2, At(10, 25, 10));
        await GateOutSecondAsync(scenario, At(10, 26, 10));

        var result = await Throughput(scenario.DbContext)
            .HandleAsync(new ThroughputQuery(new DateOnly(2026, 10, 24), _today), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [
                new ThroughputRow(new DateOnly(2026, 10, 24), "CSQ", "Operator CSQ", 1, 0),
                new ThroughputRow(new DateOnly(2026, 10, 25), "ABC", "Operator ABC", 1, 0),
                new ThroughputRow(_today, "ABC", "Operator ABC", 0, 1)
            ],
            result.Value);
    }

    [Fact]
    public async Task Throughput_CountsAContainerThatCameInAndLeftTheSameDay_InBothColumns()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.SecondId, 1, At(10, 26, 8));
        await GateOutSecondAsync(scenario, At(10, 26, 11));

        var result = await Throughput(scenario.DbContext)
            .HandleAsync(new ThroughputQuery(_today, _today), _ct);

        Assert.Equal([new ThroughputRow(_today, "ABC", "Operator ABC", 1, 1)], result.Value);
    }

    [Fact]
    public async Task Throughput_LeavesOutDaysOutsideThePeriod_ButKeepsBothEnds()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 24, 23));
        await GateInAsync(scenario, scenario.SecondId, 2, At(10, 25, 0));

        var result = await Throughput(scenario.DbContext)
            .HandleAsync(new ThroughputQuery(new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25)), _ct);

        Assert.Equal([new ThroughputRow(new DateOnly(2026, 10, 25), "ABC", "Operator ABC", 1, 0)], result.Value);
    }

    [Fact]
    public async Task Throughput_ReturnsEmptyList_WhenNothingMoved()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Throughput(dbContext).HandleAsync(new ThroughputQuery(_today, _today), _ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Throughput_Fails_WhenFromIsAfterTo()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Throughput(dbContext)
            .HandleAsync(new ThroughputQuery(_today, _today.AddDays(-1)), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("DateRange.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Throughput_UsesTheDepotDay_NotTheUtcDay()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 24, 20));
        var vietnam = new ZoneClock(TimeSpan.FromHours(7), _now);

        var result = await Throughput(scenario.DbContext, vietnam)
            .HandleAsync(new ThroughputQuery(new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25)), _ct);

        Assert.Equal([new ThroughputRow(new DateOnly(2026, 10, 25), "CSQ", "Operator CSQ", 1, 0)], result.Value);
    }

    [Fact]
    public async Task YardStock_SplitsAtTenDays_PerOperator()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 16));
        await GateInAsync(scenario, scenario.SecondId, 2, At(10, 17));

        var result = await YardStock(scenario.DbContext).HandleAsync(new YardStockQuery(), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [
                new YardStockRow("ABC", "Operator ABC", 1, 0, 1),
                new YardStockRow("CSQ", "Operator CSQ", 0, 1, 1)
            ],
            result.Value);
    }

    [Fact]
    public async Task YardStock_CountsAContainerThatCameInToday_AsRecent()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 26, 8));

        var result = await YardStock(scenario.DbContext).HandleAsync(new YardStockQuery(_today), _ct);

        Assert.Equal([new YardStockRow("CSQ", "Operator CSQ", 1, 0, 1)], result.Value);
    }

    [Fact]
    public async Task YardStock_LeavesOutContainersThatHaveGateOut()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 20));
        await GateInAsync(scenario, scenario.SecondId, 2, At(10, 20));
        await GateOutSecondAsync(scenario, At(10, 26, 10));

        var result = await YardStock(scenario.DbContext).HandleAsync(new YardStockQuery(), _ct);

        Assert.Equal([new YardStockRow("CSQ", "Operator CSQ", 1, 0, 1)], result.Value);
    }

    [Fact]
    public async Task YardStock_OnAPastDay_CountsWhatWasInTheYardThen()
    {
        await using var scenario = await CreateScenarioAsync();
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 20));
        await GateInAsync(scenario, scenario.SecondId, 2, At(10, 22));
        await GateOutSecondAsync(scenario, At(10, 26, 10));

        var onThe21st = await YardStock(scenario.DbContext).HandleAsync(new YardStockQuery(new DateOnly(2026, 10, 21)), _ct);
        var onThe25th = await YardStock(scenario.DbContext).HandleAsync(new YardStockQuery(new DateOnly(2026, 10, 25)), _ct);

        Assert.Equal([new YardStockRow("CSQ", "Operator CSQ", 1, 0, 1)], onThe21st.Value);
        Assert.Equal(2, onThe25th.Value.Sum(x => x.Total));
    }

    [Fact]
    public async Task YardStock_Fails_WhenTheDateIsInTheFuture()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await YardStock(dbContext).HandleAsync(new YardStockQuery(_today.AddDays(1)), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("ReportDate.InFuture", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task YardStock_UsesTheDepotDay_ToCountTheDaysInTheYard()
    {
        await using var scenario = await CreateScenarioAsync();
        // 2026-10-16 17:30 UTC is already the 17th in Vietnam, so on the 26th it has stood 9 days, not 10.
        await GateInAsync(scenario, scenario.Yard.ContainerId, 1, At(10, 16, 17).AddMinutes(30));
        var vietnam = new ZoneClock(TimeSpan.FromHours(7), _now);

        var result = await YardStock(scenario.DbContext, vietnam).HandleAsync(new YardStockQuery(), _ct);

        Assert.Equal([new YardStockRow("CSQ", "Operator CSQ", 1, 0, 1)], result.Value);
    }

    private sealed class ZoneClock(TimeSpan offset, DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone("Depot", offset, "Depot", "Depot");

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
