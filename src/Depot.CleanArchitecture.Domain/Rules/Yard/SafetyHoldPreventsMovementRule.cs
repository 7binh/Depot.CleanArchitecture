namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;

public sealed class SafetyHoldPreventsMovementRule(IReadOnlyCollection<StayHold> holds) : IBusinessRule
{
    public string RuleCode => "Yard.SafetyHoldPreventsMovement";
    public string Message => "Container đang bị khóa an toàn kỹ thuật nghiêm trọng, không thể đảo chuyển vị trí thông thường.";
    public bool IsBroken() => holds.Any(h => h.IsActive && h.HoldCode == "SAFETY_HOLD");
}
