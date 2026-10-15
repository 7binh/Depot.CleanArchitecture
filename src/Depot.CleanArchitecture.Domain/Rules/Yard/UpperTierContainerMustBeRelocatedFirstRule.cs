namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public sealed class UpperTierContainerMustBeRelocatedFirstRule(int targetSlotTier, int upperTierOccupiedSlotsCount) : IBusinessRule
{
    public string RuleCode => "Yard.UpperTierContainerMustBeRelocatedFirst";
    public string Message => $"Không thể rút container ở tầng {targetSlotTier} khi các tầng trên đang có {upperTierOccupiedSlotsCount} container đè. Bắt buộc phải đảo chuyển container tầng trên sang vị trí đệm trước (Cascading Relocation).";
    public bool IsBroken() => upperTierOccupiedSlotsCount > 0;
}
