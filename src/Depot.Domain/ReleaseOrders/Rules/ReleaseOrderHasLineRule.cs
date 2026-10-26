using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-GO-07: the order must have a line for the container's type and size.</summary>
public sealed class ReleaseOrderHasLineRule(ReleaseOrderItem? item) : IBusinessRule
{
    public string RuleCode => "ReleaseOrder.NoMatchingLine";
    public string Message => "The release order has no line for this container's type and size.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => item is null;
}
