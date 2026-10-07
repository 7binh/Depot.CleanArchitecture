namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class VirtualBlockCapacityRule : IBusinessRule
{
    private readonly int _currentCount;
    private readonly int _maxCapacity;

    public VirtualBlockCapacityRule(int currentCount, int maxCapacity)
    {
        _currentCount = currentCount;
        _maxCapacity = maxCapacity;
    }

    public string RuleCode => "Yard.VirtualBlockCapacityExceeded";
    public string Message => $"Khu vực bãi ảo đã đầy sức chứa (Đang chứa: {_currentCount}/{_maxCapacity} cont)!";
    public bool IsBroken() => _currentCount >= _maxCapacity;
}
