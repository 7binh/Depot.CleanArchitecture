namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Enums;

public class DamagedContainerCannotBeAllocatedRule : IBusinessRule
{
    private readonly ContainerGrade _grade;

    public DamagedContainerCannotBeAllocatedRule(ContainerGrade grade)
    {
        _grade = grade;
    }

    public string RuleCode => "Container.DamagedCannotBeAllocated";
    public string Message => "Container đang bị hư hỏng (Grade D) hoặc phế thải (Grade E), tuyệt đối không được phép cấp cho khách hàng!";
    public bool IsBroken() => _grade == ContainerGrade.GradeD_Damaged || _grade == ContainerGrade.GradeE_Scrap;
}
