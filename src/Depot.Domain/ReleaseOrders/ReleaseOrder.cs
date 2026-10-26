using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers;
using Depot.Domain.Containers.Rules;
using Depot.Domain.ReleaseOrders.Rules;

namespace Depot.Domain.ReleaseOrders;

/// <summary>
/// An order from a line operator allowing the depot to hand empty containers to a customer (BR-RO).
/// Its status (not delivered, delivering, delivered, expired) is derived, not stored (BR-RO-09).
/// </summary>
public sealed class ReleaseOrder : AuditableEntity
{
    public const int OrderNoMaxLength = 50;
    public const int ExportVesselMaxLength = 100;

    private readonly List<ReleaseOrderItem> _items = [];

    // Only for EF Core: it cannot bind the constructor below because of the `lines` parameter.
    private ReleaseOrder()
    {
    }

    private ReleaseOrder(
        Guid lineOperatorId,
        Guid customerId,
        string orderNo,
        DateOnly issuedDate,
        DateOnly expiryDate,
        string exportVessel)
    {
        LineOperatorId = lineOperatorId;
        CustomerId = customerId;
        OrderNo = orderNo;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        ExportVessel = exportVessel;
    }

    public Guid LineOperatorId { get; private set; }

    public Guid CustomerId { get; private set; }

    /// <summary>Unique per line operator, not globally (BR-RO-03).</summary>
    public string OrderNo { get; private set; } = string.Empty;

    public DateOnly IssuedDate { get; private set; }

    /// <summary>Last day empty containers may leave; delivering on this day is still valid (BR-RO-04).</summary>
    public DateOnly ExpiryDate { get; private set; }

    public string ExportVessel { get; private set; } = string.Empty;

    public IReadOnlyCollection<ReleaseOrderItem> Items => _items;

    public static Result<ReleaseOrder> Create(
        Guid lineOperatorId,
        Guid customerId,
        string? orderNo,
        DateOnly issuedDate,
        DateOnly expiryDate,
        string? exportVessel,
        IReadOnlyCollection<ReleaseOrderLine> lines)
    {
        var cleanOrderNo = orderNo?.Trim();
        var cleanVessel = exportVessel?.Trim();

        var check = BusinessRuleValidator.Check(
            [
                new NotEmptyRule(cleanOrderNo, "OrderNo"),
                new MaxLengthRule(cleanOrderNo, OrderNoMaxLength, "OrderNo"),
                new NotEmptyRule(cleanVessel, "ExportVessel"),
                new MaxLengthRule(cleanVessel, ExportVesselMaxLength, "ExportVessel"),
                new ExpiryNotBeforeIssuedRule(issuedDate, expiryDate),
                new HasLinesRule(lines.Count),
                .. lines.SelectMany(LineRules),
                new UniqueLinesRule(lines)
            ]);

        if (check.IsFailure)
        {
            return Result.Failure<ReleaseOrder>(check.Error);
        }

        var order = new ReleaseOrder(lineOperatorId, customerId, cleanOrderNo!, issuedDate, expiryDate, cleanVessel!);
        order._items.AddRange(lines.Select(x => new ReleaseOrderItem(order.Id, x.ContainerTypeId, x.SizeFeet, x.Quantity)));
        return Result.Success(order);
    }

    /// <summary>True once <paramref name="today"/> is after the expiry date (BR-RO-05).</summary>
    public bool IsExpiredOn(DateOnly today) => today > ExpiryDate;

    /// <summary>The line that covers this container's type and size, or null when the order has none (BR-GO-07).</summary>
    public ReleaseOrderItem? FindItemFor(Container container)
    {
        return _items.FirstOrDefault(x => x.ContainerTypeId == container.ContainerTypeId && x.SizeFeet == container.SizeFeet);
    }

    private static IEnumerable<IBusinessRule> LineRules(ReleaseOrderLine line)
    {
        yield return new SizeFeetRule(line.SizeFeet);
        yield return new PositiveQuantityRule(line.Quantity);
    }
}
