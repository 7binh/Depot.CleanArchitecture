namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class DeliveryOrderNotExpiredRule : IBusinessRule
{
    private readonly DateOnly _expirationDate;
    private readonly DateOnly _currentDate;

    public DeliveryOrderNotExpiredRule(DateOnly expirationDate, DateOnly currentDate)
    {
        _expirationDate = expirationDate;
        _currentDate = currentDate;
    }

    public string RuleCode => "DeliveryOrder.Expired";
    public string Message => $"Lệnh giao container đã hết hạn vào ngày {_expirationDate:dd/MM/yyyy}! Vui lòng gia hạn với Hãng tàu.";
    public bool IsBroken() => _currentDate > _expirationDate;
}
