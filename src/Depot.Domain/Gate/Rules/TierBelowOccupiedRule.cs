using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-YD-06: a container can only be stacked on a tier that already holds one.</summary>
public sealed class TierBelowOccupiedRule(int? tier, IReadOnlyCollection<int> occupiedTiers) : IBusinessRule
{
    public string RuleCode => "Tier.BelowEmpty";
    public string Message => "The tier below is empty.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => tier > 1 && !occupiedTiers.Contains(tier.Value - 1);
}
