using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-02 (A-04): 3 owner letters, 1 category letter (U, J or Z), 6 serial digits, 1 check digit.</summary>
public sealed class ContainerNumberFormatRule(string? value) : IBusinessRule
{
    private const int _ownerCodeLength = 3;
    private const int _categoryIndex = 3;
    private const int _serialStart = 4;

    public string RuleCode => "ContainerNumber.InvalidFormat";
    public string Message => "ContainerNumber must be 3 owner letters, a category letter (U, J or Z) and 7 digits.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => value is not null && !HasValidFormat(value);

    internal static bool HasValidFormat(string? text)
    {
        return text is { Length: Container.NumberLength }
            && text[.._ownerCodeLength].All(char.IsAsciiLetterUpper)
            && text[_categoryIndex] is 'U' or 'J' or 'Z'
            && text[_serialStart..].All(char.IsAsciiDigit);
    }
}
