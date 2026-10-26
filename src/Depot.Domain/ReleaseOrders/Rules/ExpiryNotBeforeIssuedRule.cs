using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-RO-07: the expiry date is not before the issued date.</summary>
public sealed class ExpiryNotBeforeIssuedRule(DateOnly issuedDate, DateOnly expiryDate) : IBusinessRule
{
    public string RuleCode => "ExpiryDate.BeforeIssuedDate";
    public string Message => "ExpiryDate must not be before IssuedDate.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => expiryDate < issuedDate;
}
