using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GI-03: a container that is already in the yard cannot be gated in again.</summary>
public sealed class ContainerNotInYardRule(bool hasOpenStay) : IBusinessRule
{
    public string RuleCode => "Container.AlreadyInYard";
    public string Message => "The container is already in the yard.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => hasOpenStay;
}
