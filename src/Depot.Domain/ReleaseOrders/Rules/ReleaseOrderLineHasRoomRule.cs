using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-RO-06: the containers delivered on a line never exceed the quantity on the order.</summary>
public sealed class ReleaseOrderLineHasRoomRule(ReleaseOrderItem? item, int deliveredOnLine) : IBusinessRule
{
    public string RuleCode => "ReleaseOrder.LineFull";
    public string Message => "The release order line has already been delivered in full.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => item is not null && deliveredOnLine >= item.Quantity;
}
