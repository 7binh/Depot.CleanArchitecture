using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>
/// BR-YD-03: odd bays take 20ft containers, even bays take 40ft containers.
/// The floor space a 40ft container also takes in the odd bays beside it is checked by
/// <see cref="NeighbourBaysFreeRule"/>.
/// </summary>
public sealed class BayMatchesSizeRule(Bay? bay, int sizeFeet) : IBusinessRule
{
    public string RuleCode => "Bay.SizeMismatch";
    public string Message => "Odd bays take 20ft containers and even bays take 40ft containers.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        if (bay is null)
        {
            return false;
        }

        var expectedSize = bay.BayNo % 2 == 1 ? Container.SizeSmallFeet : Container.SizeLargeFeet;
        return sizeFeet != expectedSize;
    }
}
