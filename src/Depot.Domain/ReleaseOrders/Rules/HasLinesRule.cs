using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-RO-02: a release order has at least one line.</summary>
public sealed class HasLinesRule(int lineCount) : IBusinessRule
{
    public string RuleCode => "Lines.Empty";
    public string Message => "A release order needs at least one line.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => lineCount == 0;
}
