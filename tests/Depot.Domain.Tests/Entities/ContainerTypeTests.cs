using Depot.Domain.Catalogs;
using Depot.Domain.Common;

namespace Depot.Domain.Tests.Entities;

public sealed class ContainerTypeTests
{
    [Fact]
    public void Create_TrimsAndUppercasesCode_WhenInputIsValid()
    {
        var result = ContainerType.Create(" dry ", " Dry Container ");

        Assert.True(result.IsSuccess);
        Assert.Equal("DRY", result.Value.Code);
        Assert.Equal("Dry Container", result.Value.Name);
    }

    [Theory]
    [InlineData("RF")]
    [InlineData("ABCDEFGHIJKLMNO")]
    public void Create_Succeeds_AtCodeLengthBoundaries(string code)
    {
        var result = ContainerType.Create(code, "Reefer");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_Fails_WhenCodeIsTooShortAfterTrim()
    {
        var result = ContainerType.Create(" R ", "Reefer");

        Assert.True(result.IsFailure);
        Assert.Equal("Code.TooShort", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_Fails_WhenCodeIsTooLong()
    {
        var result = ContainerType.Create("ABCDEFGHIJKLMNOP", "Reefer");

        Assert.True(result.IsFailure);
        Assert.Equal("Code.TooLong", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenCodeIsMissing(string? code)
    {
        var result = ContainerType.Create(code, "Reefer");

        Assert.True(result.IsFailure);
        Assert.Equal("Code.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_Fails_WhenNameIsTooLong()
    {
        var result = ContainerType.Create("RF", new string('a', 101));

        Assert.True(result.IsFailure);
        Assert.Equal("Name.TooLong", result.Error.Code);
    }
}
