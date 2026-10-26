using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>A range of bays runs from a first to a last bay number, both valid and in order.</summary>
public sealed class BayRangeRule(int fromBay, int toBay) : IBusinessRule
{
    public string RuleCode => "BayRange.Invalid";
    public string Message => $"The bay range must satisfy 1 <= FromBay <= ToBay <= {Bay.MaxBayNo}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => fromBay < 1 || toBay > Bay.MaxBayNo || fromBay > toBay;
}
