using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-03 (A-05): the last digit must equal the ISO 6346 check digit of the first 10 characters.</summary>
public sealed class ContainerNumberCheckDigitRule(string? value) : IBusinessRule
{
    private const int _modulus = 11;
    private const int _checkDigitIndex = 10;

    // Values of A..Z; multiples of 11 are skipped (BR_DEPOT 6.1).
    private static readonly int[] _letterValues =
    [
        10, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32, 34, 35, 36, 37, 38,
    ];

    public string RuleCode => "ContainerNumber.InvalidCheckDigit";
    public string Message => "ContainerNumber has a wrong check digit (ISO 6346).";
    public ErrorType Type => ErrorType.Validation;

    // A malformed number is reported by ContainerNumberFormatRule, so this rule only judges well-formed ones.
    public bool IsBroken()
    {
        return ContainerNumberFormatRule.HasValidFormat(value)
            && value![_checkDigitIndex] - '0' != ComputeCheckDigit(value);
    }

    public static int ComputeCheckDigit(string number)
    {
        var sum = 0;
        for (var i = 0; i < _checkDigitIndex; i++)
        {
            sum += CharValue(number[i]) * (1 << i);
        }

        return sum % _modulus % 10;
    }

    private static int CharValue(char c) => char.IsAsciiDigit(c) ? c - '0' : _letterValues[c - 'A'];
}
