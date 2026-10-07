namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class SizeStackingSafetyRule : IBusinessRule
{
    private readonly int _topContainerSize;
    private readonly int? _bottomContainerSize;

    public SizeStackingSafetyRule(int topContainerSize, int? bottomContainerSize)
    {
        _topContainerSize = topContainerSize;
        _bottomContainerSize = bottomContainerSize;
    }

    public string RuleCode => "Yard.InvalidSizeStacking";
    public string Message => "Tuyệt đối cấm đặt container 20ft lên nóc container 40ft (nguy cơ sập trần vỏ container bên dưới)!";
    public bool IsBroken() => _topContainerSize == 20 && _bottomContainerSize == 40;
}
