using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-RO-05: no container leaves on an expired order; leaving on the expiry day itself is valid (BR-RO-04).</summary>
public sealed class ReleaseOrderNotExpiredRule(ReleaseOrder order, DateOnly gateOutDate) : IBusinessRule
{
    public string RuleCode => "ReleaseOrder.Expired";
    public string Message => "The release order has expired.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => order.IsExpiredOn(gateOutDate);
}
