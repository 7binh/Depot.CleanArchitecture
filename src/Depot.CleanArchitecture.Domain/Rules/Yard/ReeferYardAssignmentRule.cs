namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class ReeferYardAssignmentRule : IBusinessRule
{
    private readonly bool _isReefer;
    private readonly bool _hasReeferPower;
    private readonly int _tier;
    private readonly int _maxReeferTier;

    public ReeferYardAssignmentRule(bool isReefer, bool hasReeferPower, int tier, int maxReeferTier = 4)
    {
        _isReefer = isReefer;
        _hasReeferPower = hasReeferPower;
        _tier = tier;
        _maxReeferTier = maxReeferTier;
    }

    public string RuleCode => "Yard.ReeferRequiresPowerReceptacle";
    public string Message => $"Container lạnh bắt buộc phải xếp vào Block có giàn cắm điện lạnh và chiều cao tối đa không quá {_maxReeferTier} tầng!";
    public bool IsBroken() => _isReefer && (!_hasReeferPower || _tier > _maxReeferTier);
}
