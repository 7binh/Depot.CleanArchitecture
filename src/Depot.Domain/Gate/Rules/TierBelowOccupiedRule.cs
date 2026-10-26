using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>
/// BR-YD-06: a container can only be stacked on a tier that already holds one. A 40ft container in an even bay may
/// also rest on two 20ft containers, one in each odd bay beside it; a 20ft container never rests on a 40ft one.
/// </summary>
public sealed class TierBelowOccupiedRule(
    Bay? bay,
    int? tier,
    IReadOnlyCollection<int> occupiedTiers,
    NeighbourTiers neighbours) : IBusinessRule
{
    public string RuleCode => "Tier.BelowEmpty";
    public string Message => "The tier below is empty; a 40ft container needs a 40ft container or two 20ft containers under it.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        if (tier is not > 1)
        {
            return false;
        }

        var below = tier.Value - 1;
        return !occupiedTiers.Contains(below) && !RestsOnTwoTwentyFeet(below);
    }

    private bool RestsOnTwoTwentyFeet(int below)
    {
        return bay is not null
            && bay.BayNo % 2 == 0
            && neighbours.Before.Contains(below)
            && neighbours.After.Contains(below);
    }
}
