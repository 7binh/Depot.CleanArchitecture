namespace Depot.CleanArchitecture.Domain.UnitTests.ValueObjects;

using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerNumberTests
{
    [Theory]
    [InlineData("CSQU3054383", "CSQ", 'U', "305438", '3')] // Số cont chuẩn quốc tế hợp lệ
    [InlineData("CSQU4000000", "CSQ", 'U', "400000", '0')] // Trường hợp Modulo 11 dư 10 -> CheckDigit là 0
    [InlineData("EMCU8717530", "EMC", 'U', "871753", '0')] // Ví dụ thực tế từ mentor (Evergreen Marine)
    public void Create_WithValidContainerNumber_ShouldSucceed(
        string raw, string expectedOwner, char expectedCategory, string expectedSerial, char expectedCheckDigit)
    {
        var result = ContainerNumber.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(raw);
        result.Value.OwnerCode.Should().Be(expectedOwner);
        result.Value.CategoryIdentifier.Should().Be(expectedCategory);
        result.Value.SerialNumber.Should().Be(expectedSerial);
        result.Value.CheckDigit.Should().Be(expectedCheckDigit);
    }

    [Theory]
    [InlineData("CSQU3054389")] // CSQU3054383 số đúng là 3, đổi thành 9
    [InlineData("EMCU8717539")] // EMCU8717530 số đúng là 0, đổi thành 9
    public void Create_WithInvalidCheckDigit_ShouldFail(string raw)
    {
        var result = ContainerNumber.Create(raw);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidCheckDigit");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CMAU12345")] // Thiếu ký tự
    [InlineData("CMAU1234567890")] // Dư ký tự
    public void Create_WithInvalidLength_ShouldFail(string raw)
    {
        var result = ContainerNumber.Create(raw);
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678901")] // Full số
    [InlineData("ABCDEFGHIJK")] // Full chữ
    [InlineData("CSQU400000X")] // Chứa ký tự 'X' ở vị trí check digit -> Không hợp lệ
    [InlineData("CSQA3054383")] // Category là 'A' (không thuộc U, J, Z)
    [InlineData("CSQ13054383")] // Ký tự thứ 4 là số
    [InlineData("123U3054383")] // 3 ký tự đầu là số
    public void Create_WithInvalidFormat_ShouldFail(string raw)
    {
        var result = ContainerNumber.Create(raw);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidFormat");
    }

    [Fact]
    public void TwoContainerNumbers_WithSameValue_ShouldBeEqual()
    {
        var cont1 = ContainerNumber.Create("CSQU3054383").Value;
        var cont2 = ContainerNumber.Create("CSQU3054383").Value;

        cont1.Should().Be(cont2);
        (cont1 == cont2 || cont1.Equals(cont2)).Should().BeTrue();
    }
}
