using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers;
using Depot.Domain.Gate.Rules;
using Depot.Domain.ReleaseOrders.Rules;

namespace Depot.Domain.Gate;

/// <summary>
/// One stay of a container in the depot, opened at gate in (BR-GI-01, BR-GI-04).
/// It is closed by <see cref="GateOut"/> when the container leaves on a release order (BR-GO).
/// </summary>
public sealed class ContainerStay : AuditableEntity
{
    public const int DamageNoteMaxLength = 500;
    public const int VehicleNumberMaxLength = 20;

    // Only for EF Core and for the object initializer in GateIn.
    private ContainerStay()
    {
    }

    public Guid ContainerId { get; private set; }

    public DateTimeOffset GateInAt { get; private set; }

    public StayGrade Grade { get; private set; }

    public StayCondition Condition { get; private set; }

    /// <summary>Required when the container is damaged (BR-GI-06); empty otherwise.</summary>
    public string? DamageNote { get; private set; }

    public VehicleType InVehicleType { get; private set; }

    public string InVehicleNumber { get; private set; } = string.Empty;

    public Guid BlockId { get; private set; }

    /// <summary>Empty in a virtual block.</summary>
    public Guid? RowId { get; private set; }

    /// <summary>Empty in a virtual block; 1 is the bottom tier.</summary>
    public int? Tier { get; private set; }

    /// <summary>Empty while the container is in the yard (BR-GO-01).</summary>
    public DateTimeOffset? GateOutAt { get; private set; }

    public VehicleType? OutVehicleType { get; private set; }

    public string? OutVehicleNumber { get; private set; }

    /// <summary>The release order line this container left on; the order itself is found through the line (BR-GO-02).</summary>
    public Guid? ReleaseOrderItemId { get; private set; }

    /// <summary>True while the container is still in the yard.</summary>
    public bool IsOpen => GateOutAt is null;

    /// <summary>
    /// Opens a stay. <paramref name="hasOpenStay"/> says whether the container is already in the yard;
    /// <paramref name="now"/> comes from the caller so the Domain never reads the clock.
    /// </summary>
    public static Result<ContainerStay> GateIn(
        Container container,
        bool hasOpenStay,
        GateInDetails details,
        GateInPlacement placement,
        DateTimeOffset now)
    {
        var vehicleNumber = details.InVehicle.Number?.Trim();
        var damageNote = details.DamageNote?.Trim();

        var check = BusinessRuleValidator.Check(
            [
                new ContainerNotInYardRule(hasOpenStay),
                new GateInTimeRule(details.GateInAt, now),
                new DefinedEnumRule<StayGrade>(details.Grade, "Grade"),
                new DefinedEnumRule<StayCondition>(details.Condition, "Condition"),
                new DamageNoteRule(details.Condition, damageNote),
                new MaxLengthRule(damageNote, DamageNoteMaxLength, "DamageNote"),
                new DefinedEnumRule<VehicleType>(details.InVehicle.Type, "InVehicleType"),
                new NotEmptyRule(vehicleNumber, "InVehicleNumber"),
                new MaxLengthRule(vehicleNumber, VehicleNumberMaxLength, "InVehicleNumber"),
                .. PlacementRules(container, placement)
            ]);

        if (check.IsFailure)
        {
            return Result.Failure<ContainerStay>(check.Error);
        }

        return Result.Success(new ContainerStay
        {
            ContainerId = container.Id,
            GateInAt = details.GateInAt,
            Grade = details.Grade,
            Condition = details.Condition,
            DamageNote = details.Condition == StayCondition.Damaged ? damageNote : null,
            InVehicleType = details.InVehicle.Type,
            InVehicleNumber = vehicleNumber!,
            BlockId = placement.Block.Id,
            RowId = placement.Row?.Id,
            Tier = placement.Tier
        });
    }

    /// <summary>
    /// Closes the stay when the container leaves on <paramref name="release"/> (BR-GO-01). Nothing changes on failure.
    /// <paramref name="gateOutDate"/> is the calendar day of <c>GateOutAt</c> in the depot's time zone, worked out by
    /// the caller so the Domain stays free of time zones; it decides whether the order has expired.
    /// <paramref name="hasContainerAbove"/> says whether another container in the yard sits on a higher tier of the
    /// same row; it must leave first (BR-YD-06).
    /// </summary>
    public Result GateOut(
        Container container,
        GateOutRelease release,
        GateOutDetails details,
        DateTimeOffset now,
        DateOnly gateOutDate,
        bool hasContainerAbove)
    {
        var vehicleNumber = details.OutVehicle.Number?.Trim();
        var item = release.Order.FindItemFor(container);

        var check = BusinessRuleValidator.Check(
            new StayIsOpenRule(IsOpen),
            new NothingStackedAboveRule(hasContainerAbove),
            new GateOutNotInFutureRule(details.GateOutAt, now),
            new GateOutNotBeforeGateInRule(GateInAt, details.GateOutAt),
            new DefinedEnumRule<VehicleType>(details.OutVehicle.Type, "OutVehicleType"),
            new NotEmptyRule(vehicleNumber, "OutVehicleNumber"),
            new MaxLengthRule(vehicleNumber, VehicleNumberMaxLength, "OutVehicleNumber"),
            new ReleaseOrderOperatorMatchesRule(release.Order, container),
            new ReleaseOrderNotExpiredRule(release.Order, gateOutDate),
            new ReleaseOrderHasLineRule(item),
            new ReleaseOrderLineHasRoomRule(item, release.DeliveredOnLine));

        if (check.IsFailure)
        {
            return check;
        }

        GateOutAt = details.GateOutAt;
        OutVehicleType = details.OutVehicle.Type;
        OutVehicleNumber = vehicleNumber;
        ReleaseOrderItemId = item!.Id;
        return Result.Success();
    }

    // Every rule treats a missing bay, row or tier as "not my business"; RowAndTierRequiredRule reports it first.
    private static IBusinessRule[] PlacementRules(Container container, GateInPlacement placement)
    {
        return
        [
            new VirtualBlockHasNoPositionRule(placement.Block, placement.Row, placement.Tier),
            new RowAndTierRequiredRule(placement.Block, placement.Row, placement.Tier),
            new PlacementConsistentRule(placement.Block, placement.Bay, placement.Row),
            new TierWithinRowRule(placement.Row, placement.Tier),
            new BayMatchesSizeRule(placement.Bay, container.SizeFeet),
            new SlotFreeRule(placement.Tier, placement.OccupiedTiers),
            new TierBelowOccupiedRule(placement.Tier, placement.OccupiedTiers)
        ];
    }
}
