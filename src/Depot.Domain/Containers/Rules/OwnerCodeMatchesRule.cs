using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-09 (A-15): the container's operator is the one whose code is the owner code of the number.</summary>
public sealed class OwnerCodeMatchesRule(string? number, string operatorCode) : IBusinessRule
{
    public string RuleCode => "ContainerNumber.OwnerMismatch";
    public string Message => $"ContainerNumber must start with the owner code {operatorCode}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => number is not null && !number.StartsWith(operatorCode, StringComparison.Ordinal);
}
