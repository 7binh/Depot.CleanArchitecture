using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard.Rules;

namespace Depot.Domain.Yard;

public sealed class YardRow : BaseEntity
{
    public const int MaxRowNo = 99;
    public const int MinTierCount = 2;
    public const int MaxTierCount = 10;

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
}
