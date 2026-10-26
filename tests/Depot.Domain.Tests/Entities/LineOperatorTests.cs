using Depot.Domain.Catalogs;
using Depot.Domain.Common;

namespace Depot.Domain.Tests.Entities;

public sealed class LineOperatorTests
{
    [Fact]
    public void Create_TrimsAndUppercasesCode_WhenInputIsValid()
    {
        var result = LineOperator.Create(" msc ", " Mediterranean Shipping ");

        Assert.True(result.IsSuccess);
        Assert.Equal("MSC", result.Value.Code);
        Assert.Equal("Mediterranean Shipping", result.Value.Name);
    }

    [Fact]
    public void Create_Fails_WhenCodeIsTooShortAfterTrim()
    {
        var result = LineOperator.Create(" MS ", "Maersk");

        Assert.True(result.IsFailure);
        Assert.Equal("Code.InvalidLength", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenCodeIsMissing(string? code)
    {
        var result = LineOperator.Create(code, "Maersk");

        Assert.True(result.IsFailure);
        Assert.Equal("Code.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_Fails_WhenNameIsTooLong()
    {
        var result = LineOperator.Create("MSK", new string('a', 201));

        Assert.True(result.IsFailure);
        Assert.Equal("Name.TooLong", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
