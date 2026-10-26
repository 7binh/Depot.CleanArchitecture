using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard.Rules;

namespace Depot.Domain.Yard;

public sealed class YardRow : AuditableEntity
{
    public const int MaxRowNo = 99;
    public const int MinTierCount = 2;
    public const int MaxTierCount = 5;

    private YardRow(Guid bayId, int rowNo, int maxTier)
    {
        BayId = bayId;
        RowNo = rowNo;
        MaxTier = maxTier;
    }

    public Guid BayId { get; private set; }

    public int RowNo { get; private set; }

    /// <summary>How many containers may be stacked in this row (BR-YD-07, at least 2).</summary>
    public int MaxTier { get; private set; }

    public static Result<YardRow> Create(Guid bayId, int rowNo, int maxTier)
    {
        var check = BusinessRuleValidator.Check(
            new RowNoRule(rowNo),
            new MaxTierRule(maxTier));

        if (check.IsFailure)
        {
            return Result.Failure<YardRow>(check.Error);
        }

        return Result.Success(new YardRow(bayId, rowNo, maxTier));
    }

    /// <summary>
    /// Says whether the row may be removed to shrink the yard; <paramref name="containerStands"/> is loaded by the
    /// caller (BR-YD-09).
    /// </summary>
    public static Result CanBeRemoved(bool containerStands)
    {
        return BusinessRuleValidator.Check(new RowIsEmptyRule(containerStands));
    }

    /// <summary>
    /// Raises or lowers the row. <paramref name="highestOccupiedTier"/> is the highest tier that holds a container in
    /// the yard (0 for none), loaded by the caller so the Domain never queries; nothing changes on failure.
    /// </summary>
    public Result ChangeMaxTier(int maxTier, int highestOccupiedTier)
    {
        var check = BusinessRuleValidator.Check(
            new MaxTierRule(maxTier),
            new MaxTierHoldsContainersRule(maxTier, highestOccupiedTier));

        if (check.IsFailure)
        {
            return check;
        }

        MaxTier = maxTier;
        return Result.Success();
    }
}
