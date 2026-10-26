using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>BR-GO-06 (A-09): the release order must come from the container's own line operator.</summary>
public sealed class ReleaseOrderOperatorMatchesRule(ReleaseOrder order, Container container) : IBusinessRule
{
    public string RuleCode => "ReleaseOrder.OperatorMismatch";
    public string Message => "The release order belongs to another line operator than the container.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => order.LineOperatorId != container.LineOperatorId;
}
