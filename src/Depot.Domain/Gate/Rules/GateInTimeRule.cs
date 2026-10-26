using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GI-10: the gate in time is not in the future.</summary>
public sealed class GateInTimeRule(DateTimeOffset gateInAt, DateTimeOffset now) : IBusinessRule
{
    public string RuleCode => "GateInAt.InFuture";
    public string Message => "GateInAt must not be in the future.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => gateInAt > now;
}
