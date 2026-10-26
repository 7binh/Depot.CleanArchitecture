using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>
/// BR-YD-06: a tier is only used while the tier below holds a container, so a container cannot leave
/// while another one is stacked above it in the same row.
/// </summary>
public sealed class NothingStackedAboveRule(bool hasContainerAbove) : IBusinessRule
{
    public string RuleCode => "Stay.HasContainerAbove";

    public string Message => "A container is stacked above this one; move it out first.";

    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => hasContainerAbove;
}
