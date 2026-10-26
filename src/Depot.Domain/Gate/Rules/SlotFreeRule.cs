using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-YD-05: a position holds at most one container.</summary>
public sealed class SlotFreeRule(int? tier, IReadOnlyCollection<int> occupiedTiers) : IBusinessRule
{
    public string RuleCode => "Position.Occupied";
    public string Message => "The position already holds a container.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => tier is not null && occupiedTiers.Contains(tier.Value);
}
