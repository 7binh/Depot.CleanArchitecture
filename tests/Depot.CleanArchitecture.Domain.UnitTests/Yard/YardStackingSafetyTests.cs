namespace Depot.CleanArchitecture.Domain.UnitTests.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using FluentAssertions;
using Xunit;

public class YardStackingSafetyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void TierMustHaveBottomSupportRule_Tier1_ShouldNotBeBroken_EvenWithoutSupport()
    {
        var rule = new TierMustHaveBottomSupportRule(tier: 1, hasBottomSupport: false);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void TierMustHaveBottomSupportRule_TierGreaterThan1_WithoutSupport_ShouldBeBroken()
    {
        var rule = new TierMustHaveBottomSupportRule(tier: 2, hasBottomSupport: false);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.TierMissingBottomSupport");
    }

    [Fact]
    public void TierMustHaveBottomSupportRule_TierGreaterThan1_WithSupport_ShouldNotBeBroken()
    {
        var rule = new TierMustHaveBottomSupportRule(tier: 3, hasBottomSupport: true);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void SizeStackingSafetyRule_Placing20ftOnTop40ft_ShouldBeBroken()
    {
        var rule = new SizeStackingSafetyRule(topContainerSize: 20, bottomContainerSize: 40);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.InvalidSizeStacking");
    }

    [Theory]
    [InlineData(20, 20)]
    [InlineData(40, 40)]
    [InlineData(40, 20)] // 40ft trên 2 cont 20ft
    public void SizeStackingSafetyRule_ValidSizeStacking_ShouldNotBeBroken(int topSize, int bottomSize)
    {
        var rule = new SizeStackingSafetyRule(topContainerSize: topSize, bottomContainerSize: bottomSize);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void BlockMaxTierExceededRule_WhenTierExceedsMax_ShouldBeBroken()
    {
        var rule = new BlockMaxTierExceededRule(tier: 5, maxTier: 4);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.MaxTierExceeded");
    }

    [Fact]
    public void BlockMaxTierExceededRule_WhenTierWithinMax_ShouldNotBeBroken()
    {
        var rule = new BlockMaxTierExceededRule(tier: 4, maxTier: 4);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void VirtualBlockCapacityRule_WhenCurrentReachesCapacity_ShouldBeBroken()
    {
        var rule = new VirtualBlockCapacityRule(currentCount: 50, maxCapacity: 50);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.VirtualBlockCapacityExceeded");
    }

    [Fact]
    public void VirtualBlockCapacityRule_WhenCurrentBelowCapacity_ShouldNotBeBroken()
    {
        var rule = new VirtualBlockCapacityRule(currentCount: 49, maxCapacity: 50);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void ReeferYardAssignmentRule_WhenReeferInBlockWithoutPower_ShouldBeBroken()
    {
        var rule = new ReeferYardAssignmentRule(isReefer: true, hasReeferPower: false, tier: 1);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.ReeferRequiresPowerReceptacle");
    }

    [Fact]
    public void ReeferYardAssignmentRule_WhenReeferExceedsTier4_ShouldBeBroken()
    {
        var rule = new ReeferYardAssignmentRule(isReefer: true, hasReeferPower: true, tier: 5);
        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void ReeferYardAssignmentRule_WhenReeferInPowerBlockAndTierValid_ShouldNotBeBroken()
    {
        var rule = new ReeferYardAssignmentRule(isReefer: true, hasReeferPower: true, tier: 3);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void OverheightStackingRule_WhenBottomContainerIsOverheight_ShouldBeBroken()
    {
        var rule = new OverheightStackingRule(bottomContainerIsOverheight: true);
        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Yard.OverheightMustBeTopTier");
    }

    [Fact]
    public void OverheightStackingRule_WhenBottomContainerIsNotOverheight_ShouldNotBeBroken()
    {
        var rule = new OverheightStackingRule(bottomContainerIsOverheight: false);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void YardBlock_CreatePhysicalAndVirtual_ShouldSetPropertiesProperly()
    {
        var physical = YardBlock.CreatePhysical(_tenantId, "A", "Block A", maxBay: 10, maxRow: 6, maxTier: 5, hasReeferPower: true);
        physical.IsVirtual.Should().BeFalse();
        physical.BlockCode.Should().Be("A");
        physical.HasReeferPower.Should().BeTrue();
        physical.MaxBay.Should().Be(10);

        var virtualBlock = YardBlock.CreateVirtual(_tenantId, "MR", "Xuong Sua Chua M&R", maxCapacity: 50);
        virtualBlock.IsVirtual.Should().BeTrue();
        virtualBlock.BlockCode.Should().Be("MR");
        virtualBlock.MaxCapacity.Should().Be(50);
        virtualBlock.HasReeferPower.Should().BeFalse();
    }

    [Fact]
    public void YardSlot_OccupyAndRelease_ShouldUpdateStateCorrectly()
    {
        var slot = YardSlot.Create(_tenantId, Guid.NewGuid(), bay: 1, row: 1, tier: 1);
        var containerId = Guid.NewGuid();

        slot.IsOccupied.Should().BeFalse();

        slot.Occupy(containerId, containerSize: 20);
        slot.IsOccupied.Should().BeTrue();
        slot.CurrentContainerId.Should().Be(containerId);
        slot.OccupiedContainerSize.Should().Be(20);

        slot.Release();
        slot.IsOccupied.Should().BeFalse();
        slot.CurrentContainerId.Should().BeNull();
        slot.OccupiedContainerSize.Should().BeNull();
    }
}
