namespace Depot.CleanArchitecture.Domain.UnitTests.Common;

using Depot.CleanArchitecture.Domain.Common.Rules;
using FluentAssertions;
using Xunit;

public class BusinessRuleValidatorTests
{
    private class SampleBrokenRule : IBusinessRule
    {
        public string RuleCode => "Test.BrokenRule";
        public string Message => "Quy tắc kiểm thử bị vi phạm.";
        public bool IsBroken() => true;
    }

    private class SampleValidRule : IBusinessRule
    {
        public string RuleCode => "Test.ValidRule";
        public string Message => "Quy tắc kiểm thử hợp lệ.";
        public bool IsBroken() => false;
    }

    [Fact]
    public void CheckRule_WhenRuleIsBroken_ShouldThrowBusinessRuleException()
    {
        var rule = new SampleBrokenRule();
        var act = () => BusinessRuleValidator.CheckRule(rule);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Test.BrokenRule*");
    }

    [Fact]
    public void CheckRule_WhenRuleIsValid_ShouldNotThrow()
    {
        var rule = new SampleValidRule();
        var act = () => BusinessRuleValidator.CheckRule(rule);

        act.Should().NotThrow();
    }
}
