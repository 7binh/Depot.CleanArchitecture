using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Containers.Rules;

namespace Depot.Domain.Tests.Entities;

public sealed class ContainerTests
{
    private const string ValidNumber = "CSQU3054383";

    private static readonly DateOnly _today = new(2026, 10, 26);

    private static LineOperator Operator(string code = "CSQ") => LineOperator.Create(code, "Operator").Value;

    private static ContainerSpec Spec(
        string? isoCode = "22G1",
        int sizeFeet = 20,
        decimal maxWeightKg = 30480,
        decimal tareWeightKg = 2200,
        DateOnly? manufactureDate = null)
    {
        return new ContainerSpec(isoCode, sizeFeet, maxWeightKg, tareWeightKg, manufactureDate ?? new DateOnly(2020, 5, 1));
    }

    private static Result<Container> Create(string? number = ValidNumber, ContainerSpec? spec = null, LineOperator? lineOperator = null)
    {
        return Container.Create(number, lineOperator ?? Operator(), Guid.NewGuid(), spec ?? Spec(), _today);
    }

    private static void AssertValidationError(Result<Container> result, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_StoresAllFields_AndTakesOperatorFromTheGivenOne()
    {
        var lineOperator = Operator();
        var typeId = Guid.NewGuid();
        var spec = Spec();

        var result = Container.Create(ValidNumber, lineOperator, typeId, spec, _today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidNumber, result.Value.Number);
        Assert.Equal(lineOperator.Id, result.Value.LineOperatorId);
        Assert.Equal(typeId, result.Value.ContainerTypeId);
        Assert.Equal("22G1", result.Value.IsoCode);
        Assert.Equal(20, result.Value.SizeFeet);
        Assert.Equal(30480, result.Value.MaxWeightKg);
        Assert.Equal(2200, result.Value.TareWeightKg);
        Assert.Equal(spec.ManufactureDate, result.Value.ManufactureDate);
    }

    [Fact]
    public void Create_TrimsAndUppercasesNumberAndIsoCode()
    {
        var result = Create("  csqu3054383 ", Spec(isoCode: " 22g1 "));

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidNumber, result.Value.Number);
        Assert.Equal("22G1", result.Value.IsoCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNumberIsEmpty(string? number)
    {
        AssertValidationError(Create(number), "Number.Empty");
    }

    [Theory]
    [InlineData("CSQU305438")]
    [InlineData("CSQU30543833")]
    [InlineData("CSQX3054383")]
    [InlineData("CS1U3054383")]
    [InlineData("CSQU30543A3")]
    public void Create_Fails_WhenNumberHasWrongFormat(string number)
    {
        AssertValidationError(Create(number), "ContainerNumber.InvalidFormat");
    }

    [Theory]
    [InlineData("CSQU3054384")]
    [InlineData("CMAU1234567")]
    public void Create_Fails_WhenCheckDigitIsWrong(string number)
    {
        AssertValidationError(Create(number), "ContainerNumber.InvalidCheckDigit");
    }

    [Theory]
    [InlineData("CSQU305438", 3)]
    [InlineData("CMAU123456", 4)]
    public void ComputeCheckDigit_FollowsIso6346(string firstTen, int expected)
    {
        Assert.Equal(expected, ContainerNumberCheckDigitRule.ComputeCheckDigit(firstTen));
    }

    [Fact]
    public void ComputeCheckDigit_ReturnsZero_WhenRemainderIsTen()
    {
        // Search a serial whose remainder is 10 so the "10 becomes 0" branch (A-05) is really exercised.
        var prefix = Enumerable.Range(0, 1_000_000)
            .Select(serial => $"CSQU{serial:D6}")
            .First(RemainderIsTen);

        Assert.Equal(0, ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix));
        Assert.True(Create(prefix + "0").IsSuccess);
    }

    private static bool RemainderIsTen(string firstTen)
    {
        // Same sum as the rule, recomputed independently from BR_DEPOT 6.1 (only the letters of CSQU are needed).
        int[] letters = [13, 30, 28, 32];
        var sum = 0;
        for (var i = 0; i < 4; i++)
        {
            sum += letters[i] * (1 << i);
        }

        for (var i = 4; i < 10; i++)
        {
            sum += (firstTen[i] - '0') * (1 << i);
        }

        return sum % 11 == 10;
    }

    [Fact]
    public void Create_Fails_WhenOwnerCodeDiffersFromTheOperator()
    {
        AssertValidationError(Create(lineOperator: Operator("ABC")), "ContainerNumber.OwnerMismatch");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Fails_WhenIsoCodeIsEmpty(string? isoCode)
    {
        AssertValidationError(Create(spec: Spec(isoCode: isoCode)), "IsoCode.Empty");
    }

    [Theory]
    [InlineData("22G")]
    [InlineData("22G12")]
    [InlineData("22-1")]
    public void Create_Fails_WhenIsoCodeIsNotFourLettersOrDigits(string isoCode)
    {
        AssertValidationError(Create(spec: Spec(isoCode: isoCode)), "IsoCode.Invalid");
    }

    [Theory]
    [InlineData(20)]
    [InlineData(40)]
    public void Create_AcceptsSizes20And40(int sizeFeet)
    {
        Assert.True(Create(spec: Spec(sizeFeet: sizeFeet)).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(45)]
    public void Create_Fails_WhenSizeIsNot20Or40(int sizeFeet)
    {
        AssertValidationError(Create(spec: Spec(sizeFeet: sizeFeet)), "SizeFeet.Invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Fails_WhenMaxWeightIsNotPositive(decimal maxWeightKg)
    {
        AssertValidationError(Create(spec: Spec(maxWeightKg: maxWeightKg)), "MaxWeightKg.Invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Fails_WhenTareWeightIsNotPositive(decimal tareWeightKg)
    {
        AssertValidationError(Create(spec: Spec(tareWeightKg: tareWeightKg)), "TareWeightKg.Invalid");
    }

    [Fact]
    public void Create_Fails_WhenTareExceedsMax()
    {
        AssertValidationError(Create(spec: Spec(maxWeightKg: 2000, tareWeightKg: 2001)), "TareWeightKg.ExceedsMax");
    }

    [Fact]
    public void Create_AcceptsTareEqualToMax()
    {
        Assert.True(Create(spec: Spec(maxWeightKg: 2000, tareWeightKg: 2000)).IsSuccess);
    }

    [Fact]
    public void Create_AcceptsManufactureDateToday()
    {
        Assert.True(Create(spec: Spec(manufactureDate: _today)).IsSuccess);
    }

    [Fact]
    public void Create_Fails_WhenManufactureDateIsInTheFuture()
    {
        AssertValidationError(Create(spec: Spec(manufactureDate: _today.AddDays(1))), "ManufactureDate.InFuture");
    }
}
