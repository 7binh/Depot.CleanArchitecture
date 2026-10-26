using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-YD-10: a virtual block has no bay, row or tier, so none may be given.</summary>
public sealed class VirtualBlockHasNoPositionRule(Block block, YardRow? row, int? tier) : IBusinessRule
{
    public string RuleCode => "Position.NotAllowedInVirtualBlock";
    public string Message => "A virtual block has no row or tier.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => block.IsVirtual && (row is not null || tier is not null);
}
