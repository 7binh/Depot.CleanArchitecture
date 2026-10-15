namespace Depot.CleanArchitecture.Domain.UnitTests.Orders;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Orders;
using FluentAssertions;
using Xunit;

public class ActiveHoldPreventsGateOutRuleTests
{
    [Fact]
    public void Rule_WhenContainerHasActiveHold_ShouldBeBroken()
    {
        var hold = StayHold.Create(Guid.NewGuid(), Guid.NewGuid(), "CUSTOMS_HOLD", "Cho kiem hoa", "HQ").Value!;
        var rule = new ActiveHoldPreventsGateOutRule([hold]);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Order.ContainerHasActiveHold");
    }

    [Fact]
    public void Rule_WhenAllHoldsAreReleased_ShouldNotBeBroken()
    {
        var hold = StayHold.Create(Guid.NewGuid(), Guid.NewGuid(), "CUSTOMS_HOLD", "Cho kiem hoa", "HQ").Value!;
        hold.Release("Da kiem hoa xong", "HQ");
        var rule = new ActiveHoldPreventsGateOutRule([hold]);

        rule.IsBroken().Should().BeFalse();
    }
}
