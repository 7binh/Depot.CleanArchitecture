using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>Our assumption, same as for gate in (BR-GI-10): the gate out time is not in the future.</summary>
public sealed class GateOutNotInFutureRule(DateTimeOffset gateOutAt, DateTimeOffset now) : IBusinessRule
{
    public string RuleCode => "GateOutAt.InFuture";
    public string Message => "GateOutAt must not be in the future.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => gateOutAt > now;
}
