using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>Our assumption: an ISO type code (for example 22G1) is exactly 4 letters or digits.</summary>
public sealed class IsoCodeRule(string? value) : IBusinessRule
{
    public string RuleCode => "IsoCode.Invalid";
    public string Message => $"IsoCode must be exactly {Container.IsoCodeLength} letters or digits.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        return value is not null
            && !(value.Length == Container.IsoCodeLength && value.All(char.IsAsciiLetterOrDigit));
    }
}
