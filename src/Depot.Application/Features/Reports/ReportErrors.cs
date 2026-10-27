using Depot.Domain.Common;

namespace Depot.Application.Features.Reports;

internal static class ReportErrors
{
    public static Error DateRangeInvalid()
    {
        return Error.Validation("DateRange.Invalid", "The 'from' date must not be after the 'to' date.");
    }

    public static Error ReportDateInFuture()
    {
        return Error.Validation("ReportDate.InFuture", "The report date must not be in the future.");
    }
}
