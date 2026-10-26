using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>BR-YD-03: bay numbers are positive integers (odd = 20ft, even = 40ft).</summary>
public sealed class BayNoRule(int bayNo) : IBusinessRule
{
    public string RuleCode => "BayNo.Invalid";
    public string Message => $"BayNo must be between 1 and {Bay.MaxBayNo}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => bayNo is < 1 or > Bay.MaxBayNo;
}
