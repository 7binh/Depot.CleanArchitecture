namespace Depot.CleanArchitecture.Domain.UnitTests.Entities;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerTests
{
    [Fact]
    public void Create_WhenMaxGrossWeightLessThanOrEqualToTareWeight_ShouldFail()
    {
        var number = ContainerNumber.Create("CSQU3054383").Value;
        var result = Container.Create(
            tenantId: Guid.NewGuid(),
            number: number,
            lineOperator: "CSQ",
            type: ContainerType.Dry,
            isoCode: "22G1",
            size: 20,
            tareWeight: 2200m,
            maxGrossWeight: 2200m, // <= TareWeight
            grade: ContainerGrade.GradeA);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidWeight");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldSucceedAndCalculatePayload()
    {
        var number = ContainerNumber.Create("CSQU3054383").Value;
        var result = Container.Create(
            tenantId: Guid.NewGuid(),
            number: number,
            lineOperator: "csq",
            type: ContainerType.Dry,
            isoCode: "22G1",
            size: 20,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            grade: ContainerGrade.GradeA);

        result.IsSuccess.Should().BeTrue();
        var container = result.Value;
        container.LineOperator.Should().Be("CSQ");
        container.PayloadWeight.Should().Be(28280m);
        container.Grade.Should().Be(ContainerGrade.GradeA);
    }

    [Fact]
    public void UpdateCondition_ShouldUpdateGradeAndDamageNotes()
    {
        var number = ContainerNumber.Create("CSQU3054383").Value;
        var container = Container.Create(
            tenantId: Guid.NewGuid(),
            number: number,
            lineOperator: "CSQ",
            type: ContainerType.Dry,
            isoCode: "22G1",
            size: 20,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            grade: ContainerGrade.GradeA).Value;

        container.UpdateCondition(ContainerGrade.GradeD_Damaged, "Thủng vách hông bên phải");

        container.Grade.Should().Be(ContainerGrade.GradeD_Damaged);
        container.DamageNotes.Should().Be("Thủng vách hông bên phải");
    }
}
