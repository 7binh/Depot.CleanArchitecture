using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers;
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

    /// <summary>The container size this bay takes (BR-YD-03).</summary>
    public int SizeFeet => BayNo % 2 == 1 ? Container.SizeSmallFeet : Container.SizeLargeFeet;

    /// <summary>
    /// First and last bay number a container of this bay's size takes on the floor: a 40ft container in an even bay
    /// also takes the odd bays beside it (BR-YD-04).
    /// </summary>
    public (int First, int Last) FloorSpan => SizeFeet == Container.SizeLargeFeet ? (BayNo - 1, BayNo + 1) : (BayNo, BayNo);

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

    /// <summary>
    /// Says whether the bay may be removed to shrink the yard; <paramref name="containerStands"/> is loaded by the
    /// caller (BR-YD-09).
    /// </summary>
    public static Result CanBeRemoved(bool containerStands)
    {
        return BusinessRuleValidator.Check(new BayIsEmptyRule(containerStands));
    }
}
