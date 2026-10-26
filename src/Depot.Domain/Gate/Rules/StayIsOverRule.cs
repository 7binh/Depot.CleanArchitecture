using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>Only a stay that is over may lose its position; a container still in the yard keeps its row (BR-YD-09).</summary>
public sealed class StayIsOverRule(bool isOpen) : IBusinessRule
{
    public string RuleCode => "Stay.StillOpen";
    public string Message => "The container is still in the yard, so its position cannot be forgotten.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => isOpen;
}
