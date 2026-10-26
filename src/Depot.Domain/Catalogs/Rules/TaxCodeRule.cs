using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Catalogs.Rules;

/// <summary>BR-RO-08 (A-17): a Vietnamese tax code has exactly 10 or 13 digits.</summary>
public sealed class TaxCodeRule(string? value) : IBusinessRule
{
    public string RuleCode => "TaxCode.Invalid";
    public string Message => "TaxCode must contain exactly 10 or 13 digits.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        return value is not null && !(HasAllowedLength(value) && value.All(char.IsAsciiDigit));
    }

    private static bool HasAllowedLength(string text)
    {
        return text.Length is Customer.ShortTaxCodeLength or Customer.LongTaxCodeLength;
    }
}
