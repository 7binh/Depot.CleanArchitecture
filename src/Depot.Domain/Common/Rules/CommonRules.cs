namespace Depot.Domain.Common.Rules;

public sealed class NotEmptyRule(string? value, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.Empty";
    public string Message => $"{fieldName} must not be empty.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => string.IsNullOrWhiteSpace(value);
}

public sealed class MaxLengthRule(string? value, int max, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.TooLong";
    public string Message => $"{fieldName} must not exceed {max} characters.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => value is not null && value.Length > max;
}
