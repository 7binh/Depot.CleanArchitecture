using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GO-08: the gate out time is not earlier than the gate in time.</summary>
public sealed class GateOutNotBeforeGateInRule(DateTimeOffset gateInAt, DateTimeOffset gateOutAt) : IBusinessRule
{
    public string RuleCode => "GateOutAt.BeforeGateIn";
    public string Message => "GateOutAt must not be before GateInAt.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => gateOutAt < gateInAt;
}
