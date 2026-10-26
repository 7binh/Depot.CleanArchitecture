using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>BR-YD-10: a virtual block does not manage Bay, Row, Tier.</summary>
public sealed class BlockHasBaysRule(bool blockIsVirtual) : IBusinessRule
{
    public string RuleCode => "Bay.BlockIsVirtual";
    public string Message => "A virtual block does not manage bays.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => blockIsVirtual;
}
