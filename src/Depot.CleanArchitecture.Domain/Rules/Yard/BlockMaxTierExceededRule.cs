namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class BlockMaxTierExceededRule : IBusinessRule
{
    private readonly int _tier;
    private readonly int _maxTier;

    public BlockMaxTierExceededRule(int tier, int maxTier)
    {
        _tier = tier;
        _maxTier = maxTier;
    }

    public string RuleCode => "Yard.MaxTierExceeded";
    public string Message => $"Không thể hạ container ở tầng {_tier} vì vượt quá chiều cao tối đa cho phép của dãy bãi ({_maxTier} tầng)!";
    public bool IsBroken() => _tier > _maxTier;
}
