namespace Depot.CleanArchitecture.Domain.UnitTests.Yard;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using FluentAssertions;
using Xunit;

public class YardRelocationRulesTests
{
    [Fact]
    public void SafetyHoldPreventsMovementRule_WhenSafetyHoldIsActive_ShouldBeBroken()
    {
        var hold = StayHold.Create(Guid.NewGuid(), Guid.NewGuid(), "SAFETY_HOLD", "Nguy co mat an toan vo", "ANTOAN").Value!;
        var rule = new SafetyHoldPreventsMovementRule([hold]);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.SafetyHoldPreventsMovement");
    }

    [Fact]
    public void UpperTierContainerMustBeRelocatedFirstRule_WhenUpperTierIsOccupied_ShouldBeBroken()
    {
        var rule = new UpperTierContainerMustBeRelocatedFirstRule(targetSlotTier: 1, upperTierOccupiedSlotsCount: 1);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.UpperTierContainerMustBeRelocatedFirst");
    }

    [Fact]
    public void UpperTierContainerMustBeRelocatedFirstRule_WhenUpperTierIsEmpty_ShouldNotBeBroken()
    {
        var rule = new UpperTierContainerMustBeRelocatedFirstRule(targetSlotTier: 1, upperTierOccupiedSlotsCount: 0);

        rule.IsBroken().Should().BeFalse();
    }
}
