namespace Depot.CleanArchitecture.Domain.UnitTests.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class YardCollisionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _blockId = Guid.NewGuid();

    [Fact]
    public void SlotCoordinate_Create_WithValidValues_ShouldSucceed()
    {
        var result = SlotCoordinate.Create(bay: 1, row: 2, tier: 3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bay.Should().Be(1);
        result.Value.Row.Should().Be(2);
        result.Value.Tier.Should().Be(3);
        result.Value.IsOddBay.Should().BeTrue();
        result.Value.IsEvenBay.Should().BeFalse();
        result.Value.ToString().Should().Be("Bay 01 - Row 02 - Tier 03");
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(-1, 1, 1)]
    public void SlotCoordinate_Create_WithNonPositiveValues_ShouldFail(int bay, int row, int tier)
    {
        var result = SlotCoordinate.Create(bay, row, tier);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Coordinate.Invalid");
    }

    [Fact]
    public void SlotCoordinate_GetOverlappingOddBays_ForEvenBay_ShouldReturnAdjacentOddBays()
    {
        var coord = SlotCoordinate.Create(bay: 2, row: 1, tier: 1).Value;

        var (prior, next) = coord.GetOverlappingOddBays();

        prior.Should().Be(1);
        next.Should().Be(3);
    }

    [Fact]
    public void SlotCoordinate_GetOverlappingOddBays_ForOddBay_ShouldThrow()
    {
        var coord = SlotCoordinate.Create(bay: 1, row: 1, tier: 1).Value;

        var act = () => coord.GetOverlappingOddBays();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SlotCoordinate_GetAdjacentEvenBays_ForOddBay_ShouldReturnAdjacentEvenBays()
    {
        var coord1 = SlotCoordinate.Create(bay: 1, row: 1, tier: 1).Value;
        coord1.GetAdjacentEvenBays().Should().Equal(2);

        var coord3 = SlotCoordinate.Create(bay: 3, row: 1, tier: 1).Value;
        coord3.GetAdjacentEvenBays().Should().Equal(2, 4);
    }

    [Fact]
    public void Placing40ftContainer_WhenPriorOddBayIsOccupied_ShouldBeBroken()
    {
        // Bay 01 đang có container 20ft
        var slot01 = YardSlot.Create(_tenantId, _blockId, bay: 1, row: 1, tier: 1);
        slot01.Occupy(Guid.NewGuid(), containerSize: 20);

        var existingOccupiedSlots = new List<YardSlot> { slot01 };
        var targetCoord = SlotCoordinate.Create(bay: 2, row: 1, tier: 1).Value;

        var rule = new EvenOddBayCollisionRule(targetCoord, containerSize: 40, existingOccupiedSlots);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.EvenOddBayCollision");
    }

    [Fact]
    public void Placing40ftContainer_WhenNextOddBayIsOccupied_ShouldBeBroken()
    {
        // Bay 03 đang có container 20ft
        var slot03 = YardSlot.Create(_tenantId, _blockId, bay: 3, row: 1, tier: 1);
        slot03.Occupy(Guid.NewGuid(), containerSize: 20);

        var existingOccupiedSlots = new List<YardSlot> { slot03 };
        var targetCoord = SlotCoordinate.Create(bay: 2, row: 1, tier: 1).Value;

        var rule = new EvenOddBayCollisionRule(targetCoord, containerSize: 40, existingOccupiedSlots);

        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void Placing40ftContainer_WhenAdjacentOddBaysAreEmpty_ShouldNotBeBroken()
    {
        // Bay 05 có container, nhưng Bay 01 & 03 trống
        var slot05 = YardSlot.Create(_tenantId, _blockId, bay: 5, row: 1, tier: 1);
        slot05.Occupy(Guid.NewGuid(), containerSize: 20);

        var existingOccupiedSlots = new List<YardSlot> { slot05 };
        var targetCoord = SlotCoordinate.Create(bay: 2, row: 1, tier: 1).Value;

        var rule = new EvenOddBayCollisionRule(targetCoord, containerSize: 40, existingOccupiedSlots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void Placing20ftContainer_WhenOverlyingEvenBayIsOccupied_ShouldBeBroken()
    {
        // Bay 02 đang có container 40ft
        var slot02 = YardSlot.Create(_tenantId, _blockId, bay: 2, row: 1, tier: 1);
        slot02.Occupy(Guid.NewGuid(), containerSize: 40);

        var existingOccupiedSlots = new List<YardSlot> { slot02 };

        // Thử đặt cont 20ft vào Bay 01 hoặc Bay 03
        var targetCoord01 = SlotCoordinate.Create(bay: 1, row: 1, tier: 1).Value;
        var rule01 = new EvenOddBayCollisionRule(targetCoord01, containerSize: 20, existingOccupiedSlots);

        var targetCoord03 = SlotCoordinate.Create(bay: 3, row: 1, tier: 1).Value;
        var rule03 = new EvenOddBayCollisionRule(targetCoord03, containerSize: 20, existingOccupiedSlots);

        rule01.IsBroken().Should().BeTrue();
        rule03.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void Placing20ftContainer_WhenOverlyingEvenBayIsEmpty_ShouldNotBeBroken()
    {
        // Bay 01 và Bay 03 đều là cont 20ft -> không xung đột với nhau
        var slot01 = YardSlot.Create(_tenantId, _blockId, bay: 1, row: 1, tier: 1);
        slot01.Occupy(Guid.NewGuid(), containerSize: 20);

        var existingOccupiedSlots = new List<YardSlot> { slot01 };
        var targetCoord03 = SlotCoordinate.Create(bay: 3, row: 1, tier: 1).Value;

        var rule = new EvenOddBayCollisionRule(targetCoord03, containerSize: 20, existingOccupiedSlots);

        rule.IsBroken().Should().BeFalse();
    }
}
