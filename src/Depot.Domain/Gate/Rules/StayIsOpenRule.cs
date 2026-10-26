using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GO-03: only a container that is in the yard can leave, and only once per stay.</summary>
public sealed class StayIsOpenRule(bool isOpen) : IBusinessRule
{
    public string RuleCode => "Stay.AlreadyClosed";
    public string Message => "The container has already left the yard.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => !isOpen;
}
