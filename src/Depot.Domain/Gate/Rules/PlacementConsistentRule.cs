using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>The row must belong to the bay and the bay to the block, otherwise the position is not one place.</summary>
public sealed class PlacementConsistentRule(Block block, Bay? bay, YardRow? row) : IBusinessRule
{
    public string RuleCode => "Position.Inconsistent";
    public string Message => "The row, bay and block do not belong together.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        if (row is null)
        {
            return false;
        }

        return bay is null || row.BayId != bay.Id || bay.BlockId != block.Id;
    }
}
