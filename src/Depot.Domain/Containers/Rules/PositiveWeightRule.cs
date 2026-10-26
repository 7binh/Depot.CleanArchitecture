using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-07: a container weight (maximum or tare) is greater than 0.</summary>
public sealed class PositiveWeightRule(decimal weightKg, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.Invalid";
    public string Message => $"{fieldName} must be greater than 0.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => weightKg <= 0;
}
