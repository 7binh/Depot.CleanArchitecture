using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-07: the tare weight does not exceed the maximum weight.</summary>
public sealed class TareNotAboveMaxRule(decimal tareWeightKg, decimal maxWeightKg) : IBusinessRule
{
    public string RuleCode => "TareWeightKg.ExceedsMax";
    public string Message => "TareWeightKg must not exceed MaxWeightKg.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => tareWeightKg > maxWeightKg;
}
