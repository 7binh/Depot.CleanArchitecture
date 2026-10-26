using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>BR-YD-07: the maximum tier of a row is declared by the user, at least 2 (upper bound is our assumption).</summary>
public sealed class MaxTierRule(int maxTier) : IBusinessRule
{
    public string RuleCode => "MaxTier.Invalid";
    public string Message => $"MaxTier must be between {YardRow.MinTierCount} and {YardRow.MaxTierCount}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => maxTier < YardRow.MinTierCount || maxTier > YardRow.MaxTierCount;
}
