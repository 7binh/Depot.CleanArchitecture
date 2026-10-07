namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class OverheightStackingRule : IBusinessRule
{
    private readonly bool _bottomContainerIsOverheight;

    public OverheightStackingRule(bool bottomContainerIsOverheight)
    {
        _bottomContainerIsOverheight = bottomContainerIsOverheight;
    }

    public string RuleCode => "Yard.OverheightMustBeTopTier";
    public string Message => "Container bên dưới là loại mở nóc hoặc quá khổ (Overheight/OOG), tuyệt đối cấm xếp container khác đè lên nóc!";
    public bool IsBroken() => _bottomContainerIsOverheight;
}
