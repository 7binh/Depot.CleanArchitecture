namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class TierMustHaveBottomSupportRule : IBusinessRule
{
    private readonly int _tier;
    private readonly bool _hasBottomSupport;

    public TierMustHaveBottomSupportRule(int tier, bool hasBottomSupport)
    {
        _tier = tier;
        _hasBottomSupport = hasBottomSupport;
    }

    public string RuleCode => "Yard.TierMissingBottomSupport";
    public string Message => $"Không thể hạ container ở tầng {_tier} vì tầng {_tier - 1} ngay bên dưới chưa có container làm bệ đỡ!";
    public bool IsBroken() => _tier > 1 && !_hasBottomSupport;
}
