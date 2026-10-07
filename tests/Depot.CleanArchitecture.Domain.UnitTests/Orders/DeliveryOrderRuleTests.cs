namespace Depot.CleanArchitecture.Domain.UnitTests.Orders;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.Rules.Orders;
using FluentAssertions;
using Xunit;

public class DeliveryOrderRuleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void DeliveryOrderNotExpiredRule_WhenExpiredYesterday_ShouldBeBroken()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rule = new DeliveryOrderNotExpiredRule(yesterday, today);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("DeliveryOrder.Expired");
    }

    [Fact]
    public void DeliveryOrderNotExpiredRule_WhenValidUntilTomorrow_ShouldNotBeBroken()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rule = new DeliveryOrderNotExpiredRule(tomorrow, today);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void ContainerLineOperatorMatchesOrderRule_WhenMismatched_ShouldBeBroken()
    {
        var rule = new ContainerLineOperatorMatchesOrderRule("MAERSK", "CMA-CGM");

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("DeliveryOrder.LineOperatorMismatch");
    }

    [Fact]
    public void ContainerLineOperatorMatchesOrderRule_WhenMatchedCaseInsensitively_ShouldNotBeBroken()
    {
        var rule = new ContainerLineOperatorMatchesOrderRule("cma-cgm", "CMA-CGM");

        rule.IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData(ContainerGrade.GradeD_Damaged)]
    [InlineData(ContainerGrade.GradeE_Scrap)]
    public void DamagedContainerCannotBeAllocatedRule_WhenDamaged_ShouldBeBroken(ContainerGrade damagedGrade)
    {
        var rule = new DamagedContainerCannotBeAllocatedRule(damagedGrade);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("Container.DamagedCannotBeAllocated");
    }

    [Theory]
    [InlineData(ContainerGrade.GradeA)]
    [InlineData(ContainerGrade.GradeB)]
    [InlineData(ContainerGrade.GradeC)]
    public void DamagedContainerCannotBeAllocatedRule_WhenSound_ShouldNotBeBroken(ContainerGrade soundGrade)
    {
        var rule = new DamagedContainerCannotBeAllocatedRule(soundGrade);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void DeliveryOrderQuotaRemainingRule_WhenDeliveredQuantityReachesOrdered_ShouldBeBroken()
    {
        var rule = new DeliveryOrderQuotaRemainingRule(deliveredQuantity: 5, orderedQuantity: 5);

        rule.IsBroken().Should().BeTrue();
        rule.RuleCode.Should().Be("DeliveryOrder.QuotaExceeded");
    }

    [Fact]
    public void DeliveryOrderQuotaRemainingRule_WhenDeliveredQuantityLessThanOrdered_ShouldNotBeBroken()
    {
        var rule = new DeliveryOrderQuotaRemainingRule(deliveredQuantity: 3, orderedQuantity: 5);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void ContainerMatchesOrderSpecificationRule_WhenSizeOrTypeOrGradeMismatch_ShouldBeBroken()
    {
        // Sai kích cỡ: Cont 20ft cho lệnh 40ft
        var sizeMismatch = new ContainerMatchesOrderSpecificationRule(
            containerSize: 20, containerType: ContainerType.Dry, containerGrade: ContainerGrade.GradeA,
            requiredSize: 40, requiredType: ContainerType.Dry, requiredGrade: ContainerGrade.GradeA);
        sizeMismatch.IsBroken().Should().BeTrue();
        sizeMismatch.RuleCode.Should().Be("DeliveryOrder.SpecificationMismatch");

        // Sai loại: Cont Dry cho lệnh Reefer
        var typeMismatch = new ContainerMatchesOrderSpecificationRule(
            containerSize: 20, containerType: ContainerType.Dry, containerGrade: ContainerGrade.GradeA,
            requiredSize: 20, requiredType: ContainerType.Reefer, requiredGrade: ContainerGrade.GradeA);
        typeMismatch.IsBroken().Should().BeTrue();

        // Sai phân hạng: Cấp Grade C khi khách yêu cầu Grade A
        var gradeMismatch = new ContainerMatchesOrderSpecificationRule(
            containerSize: 20, containerType: ContainerType.Dry, containerGrade: ContainerGrade.GradeC,
            requiredSize: 20, requiredType: ContainerType.Dry, requiredGrade: ContainerGrade.GradeA);
        gradeMismatch.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void ContainerMatchesOrderSpecificationRule_WhenSpecificationMatches_ShouldNotBeBroken()
    {
        // Khách yêu cầu Grade B, cấp Grade A (chất lượng tốt hơn)
        var betterGrade = new ContainerMatchesOrderSpecificationRule(
            containerSize: 40, containerType: ContainerType.Dry, containerGrade: ContainerGrade.GradeA,
            requiredSize: 40, requiredType: ContainerType.Dry, requiredGrade: ContainerGrade.GradeB);
        betterGrade.IsBroken().Should().BeFalse();

        // Khớp chính xác
        var exactMatch = new ContainerMatchesOrderSpecificationRule(
            containerSize: 40, containerType: ContainerType.Dry, containerGrade: ContainerGrade.GradeB,
            requiredSize: 40, requiredType: ContainerType.Dry, requiredGrade: ContainerGrade.GradeB);
        exactMatch.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void DeliveryOrder_CreateAndAddItemAndExtend_ShouldWorkCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var order = DeliveryOrder.Create(
            _tenantId,
            orderNumber: "DO123456",
            lineOperator: "ONE",
            customerName: "Cong ty TNHH Logistics ABC",
            customerTaxCode: "0312345678",
            expirationDate: today,
            vesselName: "ONE CONTINUITY",
            voyageNo: "2026W");

        order.AddItem(size: 20, ContainerType.Dry, ContainerGrade.GradeA, quantity: 5);
        order.Items.Should().HaveCount(1);
        var item = order.Items.First();
        item.OrderedQuantity.Should().Be(5);
        item.DeliveredQuantity.Should().Be(0);

        item.RecordDelivery();
        item.DeliveredQuantity.Should().Be(1);

        var newExpiration = today.AddDays(7);
        order.ExtendExpirationDate(newExpiration);
        order.ExpirationDate.Should().Be(newExpiration);
    }
}
