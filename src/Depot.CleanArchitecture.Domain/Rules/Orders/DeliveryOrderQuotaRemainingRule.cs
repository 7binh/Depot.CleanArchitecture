namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class DeliveryOrderQuotaRemainingRule : IBusinessRule
{
    private readonly int _deliveredQuantity;
    private readonly int _orderedQuantity;

    public DeliveryOrderQuotaRemainingRule(int deliveredQuantity, int orderedQuantity)
    {
        _deliveredQuantity = deliveredQuantity;
        _orderedQuantity = orderedQuantity;
    }

    public string RuleCode => "DeliveryOrder.QuotaExceeded";
    public string Message => $"Lệnh giao container đã hết định ngạch cho phép (Đã giao: {_deliveredQuantity}/{_orderedQuantity})!";
    public bool IsBroken() => _deliveredQuantity >= _orderedQuantity;
}
