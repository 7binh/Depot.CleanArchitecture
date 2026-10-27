using System.Globalization;
using Depot.Domain.Gate;

namespace Depot.Domain.Tests.Entities;

public sealed class StayDurationTests
{
    [Theory]
    [InlineData("2026-10-26", "2026-10-26", 0)]
    [InlineData("2026-10-17", "2026-10-26", 9)]
    [InlineData("2026-10-16", "2026-10-26", 10)]
    [InlineData("2026-09-30", "2026-10-02", 2)]
    public void DaysBetween_CountsCalendarDays(string gateIn, string report, int expected)
    {
        var days = StayDuration.DaysBetween(
            DateOnly.Parse(gateIn, CultureInfo.InvariantCulture),
            DateOnly.Parse(report, CultureInfo.InvariantCulture));

        Assert.Equal(expected, days);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(40, true)]
    public void IsLongStay_StartsAtTenDays(int days, bool expected)
    {
        Assert.Equal(expected, StayDuration.IsLongStay(days));
    }
}
