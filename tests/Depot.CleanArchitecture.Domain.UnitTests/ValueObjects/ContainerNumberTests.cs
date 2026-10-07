namespace Depot.CleanArchitecture.Domain.UnitTests.ValueObjects;

using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerNumberTests
{
    [Theory]
    [InlineData("CSQU3054383")] // Số cont chuẩn quốc tế hợp lệ
    public void Create_WithValidContainerNumber_ShouldSucceed(string raw)
    {
        var result = ContainerNumber.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(raw);
        result.Value.OwnerCode.Should().Be("CSQ");
        result.Value.CategoryIdentifier.Should().Be('U');
        result.Value.SerialNumber.Should().Be("305438");
        result.Value.CheckDigit.Should().Be('3');
    }

    [Fact]
    public void Create_WithInvalidCheckDigit_ShouldFail()
    {
        // CSQU3054383 số đúng là số 3 ở đuôi, ta đổi thành 9
        var result = ContainerNumber.Create("CSQU3054389");

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

    [Fact]
    public void TwoContainerNumbers_WithSameValue_ShouldBeEqual()
    {
        var cont1 = ContainerNumber.Create("CSQU3054383").Value;
        var cont2 = ContainerNumber.Create("CSQU3054383").Value;

        cont1.Should().Be(cont2);
        (cont1 == cont2 || cont1.Equals(cont2)).Should().BeTrue();
    }
}
