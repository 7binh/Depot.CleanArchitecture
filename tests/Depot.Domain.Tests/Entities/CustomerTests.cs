using Depot.Domain.Catalogs;
using Depot.Domain.Common;

namespace Depot.Domain.Tests.Entities;

public sealed class CustomerTests
{
    [Fact]
    public void Create_TrimsInput_WhenInputIsValid()
    {
        var result = Customer.Create(" 0101234567 ", " Cong ty ABC ");

        Assert.True(result.IsSuccess);
        Assert.Equal("0101234567", result.Value.TaxCode);
        Assert.Equal("Cong ty ABC", result.Value.Name);
    }

    [Theory]
    [InlineData("0101234567")]
    [InlineData("0101234567001")]
    public void Create_Succeeds_WithTenOrThirteenDigits(string taxCode)
    {
        var result = Customer.Create(taxCode, "Cong ty ABC");

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("01012345670")]
    [InlineData("010123456700")]
    [InlineData("01012345670011")]
    [InlineData("01012345ab")]
    [InlineData("0101-23456")]
    public void Create_Fails_WhenTaxCodeIsNotTenOrThirteenDigits(string taxCode)
    {
        var result = Customer.Create(taxCode, "Cong ty ABC");

        Assert.True(result.IsFailure);
        Assert.Equal("TaxCode.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenTaxCodeIsMissing(string? taxCode)
    {
        var result = Customer.Create(taxCode, "Cong ty ABC");

        Assert.True(result.IsFailure);
        Assert.Equal("TaxCode.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_Fails_WhenNameIsMissing()
    {
        var result = Customer.Create("0101234567", "  ");

        Assert.True(result.IsFailure);
        Assert.Equal("Name.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_Fails_WhenNameIsTooLong()
    {
        var result = Customer.Create("0101234567", new string('a', 201));

        Assert.True(result.IsFailure);
        Assert.Equal("Name.TooLong", result.Error.Code);
    }
}
