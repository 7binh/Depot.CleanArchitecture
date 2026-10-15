namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;

public sealed class ActiveHoldPreventsGateOutRule(IReadOnlyCollection<StayHold> holds) : IBusinessRule
{
    public string RuleCode => "Order.ContainerHasActiveHold";
    public string Message => "Container đang có lệnh khóa giữ hiệu lực trong bãi, nghiêm cấm xuất bãi (Gate-Out). Bắt buộc giải tỏa 100% lệnh giữ.";
    public bool IsBroken() => holds.Any(h => h.IsActive);
}
