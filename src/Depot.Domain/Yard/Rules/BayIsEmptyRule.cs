using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>
/// BR-YD-09: a bay cannot be removed while a container in the yard stands in it, or on its floor space
/// (a 40ft container in an even bay beside an odd bay).
/// </summary>
public sealed class BayIsEmptyRule(bool containerStands) : IBusinessRule
{
    public string RuleCode => "Bay.HasContainers";
    public string Message => "A container still stands in this bay; move it out before removing the bay.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => containerStands;
}
