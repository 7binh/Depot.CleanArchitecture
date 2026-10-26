using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-YD-07: the tier is between 1 and the maximum tier of the row.</summary>
public sealed class TierWithinRowRule(YardRow? row, int? tier) : IBusinessRule
{
    public string RuleCode => "Tier.OutOfRange";
    public string Message => "Tier must be between 1 and the maximum tier of the row.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => row is not null && tier is not null && (tier < 1 || tier > row.MaxTier);
}
