using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>
/// BR-YD-09: a row cannot be made lower while a container in the yard stands on a tier above the new maximum.
/// <paramref name="highestOccupiedTier"/> is 0 when the row holds no container.
/// </summary>
public sealed class MaxTierHoldsContainersRule(int maxTier, int highestOccupiedTier) : IBusinessRule
{
    public string RuleCode => "MaxTier.BelowOccupied";
    public string Message => $"A container still stands on tier {highestOccupiedTier}; move it out before lowering the maximum to {maxTier}.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => highestOccupiedTier > maxTier;
}
