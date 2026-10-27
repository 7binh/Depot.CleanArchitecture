namespace Depot.Domain.Gate;

/// <summary>How long a container has stood in the depot, counted in calendar days (BR-RP-04, A-06).</summary>
public static class StayDuration
{
    /// <summary>From this many days on a container counts as a long stay; 0 to 9 days is recent.</summary>
    public const int LongStayFromDays = 10;

    /// <summary>Calendar days from the gate in day to the report day; both days are the depot's local days.</summary>
    public static int DaysBetween(DateOnly gateInDate, DateOnly reportDate)
    {
        return reportDate.DayNumber - gateInDate.DayNumber;
    }

    public static bool IsLongStay(int days)
    {
        return days >= LongStayFromDays;
    }
}
