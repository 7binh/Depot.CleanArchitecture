using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Tests.Common;

public sealed class BusinessRuleValidatorTests
{
    [Fact]
    public void Check_ReturnsValidationError_WhenRuleIsBroken()
    {
        var result = BusinessRuleValidator.Check(new NotEmptyRule("", "Code"));

        Assert.True(result.IsFailure);
        Assert.Equal("Code.Empty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Check_ReturnsFirstBrokenRule_WhenSeveralRulesAreBroken()
    {
        var result = BusinessRuleValidator.Check(
            new NotEmptyRule("", "Code"),
            new MaxLengthRule("abcdef", 3, "Name"));

        Assert.Equal("Code.Empty", result.Error?.Code);
    }

    [Fact]
    public void Check_ReturnsSuccess_WhenNoRuleIsBroken()
    {
        var result = BusinessRuleValidator.Check(
            new NotEmptyRule("abc", "Code"),
            new MaxLengthRule("abc", 3, "Name"));

        Assert.True(result.IsSuccess);
    }
}
