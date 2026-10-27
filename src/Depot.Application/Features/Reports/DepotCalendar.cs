namespace Depot.Application.Features.Reports;

/// <summary>Calendar days at the depot: a day starts and ends at local midnight, not at UTC midnight.</summary>
internal static class DepotCalendar
{
    public static DateOnly DateOf(DateTimeOffset moment, TimeZoneInfo zone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, zone).DateTime);
    }

    // In UTC because Npgsql only accepts UTC values for a timestamptz parameter.
    public static DateTimeOffset StartOf(DateOnly date, TimeZoneInfo zone)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, zone.GetUtcOffset(midnight)).ToUniversalTime();
    }
}
