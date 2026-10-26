using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-RO-02: the quantity of a line is greater than 0.</summary>
public sealed class PositiveQuantityRule(int quantity) : IBusinessRule
{
    public string RuleCode => "Quantity.Invalid";
    public string Message => "Quantity must be greater than 0.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => quantity <= 0;
}
