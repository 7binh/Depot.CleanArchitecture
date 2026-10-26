using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>
/// BR-YD-04: a 40ft container in an even bay N lies over the odd bays N-1 and N+1, so both must be declared.
/// A bay removed from the middle of the block leaves a gap that cannot hold it.
/// </summary>
public sealed class NeighbourBaysExistRule(Bay? bay, NeighbourTiers neighbours) : IBusinessRule
{
    public string RuleCode => "Bay.NeighboursMissing";
    public string Message => "A 40ft container needs the odd bays on both sides of its bay to exist.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        return bay is not null && bay.BayNo % 2 == 0 && !(neighbours.BeforeExists && neighbours.AfterExists);
    }
}
