using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>A move has to end somewhere else than it started.</summary>
public sealed class PositionChangesRule(bool isSamePosition) : IBusinessRule
{
    public string RuleCode => "Position.Unchanged";
    public string Message => "The container is already at this position.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => isSamePosition;
}
