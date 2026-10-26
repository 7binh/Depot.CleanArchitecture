using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>
/// BR-YD-04: a 40ft container in an even bay N also covers the odd bays N-1 and N+1, so a position is not free
/// while a container covers the same row and tier from a bay beside it. Two 40ft containers in even bays two
/// apart share the odd bay between them, so they exclude each other as well.
/// </summary>
public sealed class NeighbourBaysFreeRule(Bay? bay, int? tier, NeighbourTiers neighbours) : IBusinessRule
{
    public string RuleCode => "Position.OverlapsNeighbour";
    public string Message => "A container in a neighbouring bay already covers this position.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken()
    {
        if (bay is null || tier is null)
        {
            return false;
        }

        var covered = neighbours.Before.Contains(tier.Value) || neighbours.After.Contains(tier.Value);
        return covered || (bay.BayNo % 2 == 0 && IsCoveredByFarBay(tier.Value));
    }

    private bool IsCoveredByFarBay(int tierValue)
    {
        return neighbours.FarBefore.Contains(tierValue) || neighbours.FarAfter.Contains(tierValue);
    }
}
