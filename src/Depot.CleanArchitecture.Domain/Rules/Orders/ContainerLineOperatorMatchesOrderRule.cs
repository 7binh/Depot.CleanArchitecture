namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class ContainerLineOperatorMatchesOrderRule : IBusinessRule
{
    private readonly string _containerLineOperator;
    private readonly string _orderLineOperator;

    public ContainerLineOperatorMatchesOrderRule(string containerLineOperator, string orderLineOperator)
    {
        _containerLineOperator = containerLineOperator;
        _orderLineOperator = orderLineOperator;
    }

    public string RuleCode => "DeliveryOrder.LineOperatorMismatch";
    public string Message => $"Container thuộc hãng tàu '{_containerLineOperator}', không thể xuất cho Lệnh của hãng tàu '{_orderLineOperator}'!";
    public bool IsBroken() => !string.Equals(_containerLineOperator, _orderLineOperator, StringComparison.OrdinalIgnoreCase);
}
