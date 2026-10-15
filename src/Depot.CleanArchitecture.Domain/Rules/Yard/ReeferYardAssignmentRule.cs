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

    public string RuleCode => _isReefer && !_hasReeferPower
        ? "Yard.ReeferRequiresPowerReceptacle"
        : (!_isReefer && _hasReeferPower
            ? "Yard.DryContainerCannotOccupyReeferSlot"
            : "Yard.ReeferMaxTierExceeded");

    public string Message => _isReefer && !_hasReeferPower
        ? "Container lạnh (Reefer) bắt buộc phải xếp vào Block có giàn cắm điện lạnh (Block B)!"
        : (!_isReefer && _hasReeferPower
            ? "Container khô thường không được chiếm dụng ô bãi chuyên dụng có giàn cắm điện lạnh!"
            : $"Chiều cao xếp dỡ container lạnh không được vượt quá tầng {_maxReeferTier}!");

    public bool IsBroken()
    {
        if (_isReefer && !_hasReeferPower) return true;
        if (!_isReefer && _hasReeferPower) return true;
        if (_isReefer && _tier > _maxReeferTier) return true;
        return false;
    }
}
