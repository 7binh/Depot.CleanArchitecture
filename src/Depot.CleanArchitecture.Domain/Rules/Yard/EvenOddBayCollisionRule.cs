namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;

public class EvenOddBayCollisionRule : IBusinessRule
{
    private readonly SlotCoordinate _target;
    private readonly int _containerSize;
    private readonly IEnumerable<YardSlot> _occupiedSlotsInSameRowAndTier;

    public EvenOddBayCollisionRule(
        SlotCoordinate target,
        int containerSize,
        IEnumerable<YardSlot> occupiedSlotsInSameRowAndTier)
    {
        _target = target;
        _containerSize = containerSize;
        _occupiedSlotsInSameRowAndTier = occupiedSlotsInSameRowAndTier;
    }

    public string RuleCode => "Yard.EvenOddBayCollision";
    public string Message => "Không thể hạ container: Vị trí bị xung đột không gian vật lý với container 20ft/40ft ở bay liền kề!";

    public bool IsBroken()
    {
        var occupiedBays = _occupiedSlotsInSameRowAndTier.Select(s => s.Bay).ToHashSet();

        if (_containerSize == 40)
        {
            // Cont 40ft hạ vào bay chẵn: kiểm tra 2 bay lẻ trước và sau
            var (bay1, bay2) = _target.GetOverlappingOddBays();
            if (occupiedBays.Contains(bay1) || occupiedBays.Contains(bay2))
                return true; // Xung đột với cont 20ft đang có sẵn!
        }
        else if (_containerSize == 20)
        {
            // Cont 20ft hạ vào bay lẻ: kiểm tra các bay chẵn lân cận
            foreach (var evenBay in _target.GetAdjacentEvenBays())
            {
                if (occupiedBays.Contains(evenBay))
                    return true; // Xung đột với cont 40ft đang chiếm dụng khoảng không!
            }
        }

        return false;
    }
}
