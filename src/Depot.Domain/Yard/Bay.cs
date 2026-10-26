using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard.Rules;

namespace Depot.Domain.Yard;

public sealed class Bay : AuditableEntity
{
    public const int MaxBayNo = 999;

    private Bay(Guid blockId, int bayNo)
    {
        BlockId = blockId;
        BayNo = bayNo;
    }

    public Guid BlockId { get; private set; }

    /// <summary>Odd = 20ft bay, even = 40ft bay (BR-YD-03).</summary>
    public int BayNo { get; private set; }

    public static Result<Bay> Create(Block block, int bayNo)
    {
        var check = BusinessRuleValidator.Check(
            new BlockHasBaysRule(block.IsVirtual),
            new BayNoRule(bayNo));

        if (check.IsFailure)
        {
            return Result.Failure<Bay>(check.Error);
        }

        return Result.Success(new Bay(block.Id, bayNo));
    }
}
