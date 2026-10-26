namespace Depot.Domain.Common.Rules;

public sealed class NotEmptyRule(string? value, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.Empty";
    public string Message => $"{fieldName} must not be empty.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => string.IsNullOrWhiteSpace(value);
}

public sealed class ExactLengthRule(string? value, int length, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.InvalidLength";
    public string Message => $"{fieldName} must be exactly {length} characters.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => value is not null && value.Length != length;
}

public sealed class MinLengthRule(string? value, int min, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.TooShort";
    public string Message => $"{fieldName} must be at least {min} characters.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => value is not null && value.Length < min;
}

public sealed class MaxLengthRule(string? value, int max, string fieldName) : IBusinessRule
{
    public string RuleCode => $"{fieldName}.TooLong";
    public string Message => $"{fieldName} must not exceed {max} characters.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => value is not null && value.Length > max;
}
