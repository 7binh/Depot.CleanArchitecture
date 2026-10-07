namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Enums;

public class ContainerMatchesOrderSpecificationRule : IBusinessRule
{
    private readonly int _containerSize;
    private readonly ContainerType _containerType;
    private readonly ContainerGrade _containerGrade;
    private readonly int _requiredSize;
    private readonly ContainerType _requiredType;
    private readonly ContainerGrade _requiredGrade;

    public ContainerMatchesOrderSpecificationRule(
        int containerSize,
        ContainerType containerType,
        ContainerGrade containerGrade,
        int requiredSize,
        ContainerType requiredType,
        ContainerGrade requiredGrade)
    {
        _containerSize = containerSize;
        _containerType = containerType;
        _containerGrade = containerGrade;
        _requiredSize = requiredSize;
        _requiredType = requiredType;
        _requiredGrade = requiredGrade;
    }

    public string RuleCode => "DeliveryOrder.SpecificationMismatch";
    public string Message => "Quy cách container không khớp với yêu cầu trên Lệnh DO (sai kích thước, loại hoặc phân hạng chất lượng)!";

    public bool IsBroken() =>
        _containerSize != _requiredSize ||
        _containerType != _requiredType ||
        _containerGrade > _requiredGrade;
}
