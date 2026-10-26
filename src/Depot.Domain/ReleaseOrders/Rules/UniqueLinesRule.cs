using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.ReleaseOrders.Rules;

/// <summary>A release order has one line per container type and size; extra quantity goes on the same line.</summary>
public sealed class UniqueLinesRule(IEnumerable<ReleaseOrderLine> lines) : IBusinessRule
{
    public string RuleCode => "Lines.Duplicate";
    public string Message => "Each container type and size may appear only once in a release order.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken()
    {
        return lines.GroupBy(x => (x.ContainerTypeId, x.SizeFeet)).Any(group => group.Count() > 1);
    }
}
