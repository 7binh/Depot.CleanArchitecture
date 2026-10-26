using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class ContainerStayTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static Container Container(int sizeFeet = 20)
    {
        var spec = new ContainerSpec("22G1", sizeFeet, 30480, 2200, new DateOnly(2020, 5, 1));
        var lineOperator = LineOperator.Create("CSQ", "Operator").Value;
        return Domain.Containers.Container.Create("CSQU3054383", lineOperator, Guid.NewGuid(), spec, new DateOnly(2026, 10, 26)).Value;
    }

    private static GateInDetails Details(
        DateTimeOffset? gateInAt = null,
        StayGrade grade = StayGrade.A,
        StayCondition condition = StayCondition.Normal,
        string? damageNote = null,
        VehicleType vehicleType = VehicleType.Truck,
        string? vehicleNumber = "51C-12345")
    {
        return new GateInDetails(gateInAt ?? _now, grade, condition, damageNote, new Vehicle(vehicleType, vehicleNumber));
    }

    private sealed record Yard(Block Block, Bay Bay, YardRow Row);

    private static Yard NormalYard(int bayNo = 1, int maxTier = 4)
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var bay = Bay.Create(block, bayNo).Value;
        var row = YardRow.Create(bay.Id, 1, maxTier).Value;
        return new Yard(block, bay, row);
    }

    private static GateInPlacement PlaceIn(Yard yard, int? tier = 1, params int[] occupiedTiers)
    {
        return new GateInPlacement(yard.Block, yard.Bay, yard.Row, tier, occupiedTiers);
    }

    private static GateInPlacement VirtualPlacement()
    {
        var block = Block.Create("V1", "Virtual 1", true).Value;
        return new GateInPlacement(block, null, null, null, []);
    }

    private static Result<ContainerStay> GateIn(
        GateInPlacement placement,
        GateInDetails? details = null,
        Container? container = null,
        bool hasOpenStay = false)
    {
        return ContainerStay.GateIn(container ?? Container(), hasOpenStay, details ?? Details(), placement, _now);
    }

    private static void AssertError(Result<ContainerStay> result, string code, ErrorType type = ErrorType.Validation)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

    [Fact]
    public void GateIn_StoresAllFields_ForANormalBlock()
    {
        var yard = NormalYard();
        var container = Container();

        var result = GateIn(PlaceIn(yard, 1), Details(grade: StayGrade.B, vehicleType: VehicleType.Barge), container);

        Assert.True(result.IsSuccess);
        Assert.Equal(container.Id, result.Value.ContainerId);
        Assert.Equal(_now, result.Value.GateInAt);
        Assert.Equal(StayGrade.B, result.Value.Grade);
        Assert.Equal(StayCondition.Normal, result.Value.Condition);
        Assert.Null(result.Value.DamageNote);
        Assert.Equal(VehicleType.Barge, result.Value.InVehicleType);
        Assert.Equal("51C-12345", result.Value.InVehicleNumber);
        Assert.Equal(yard.Block.Id, result.Value.BlockId);
        Assert.Equal(yard.Row.Id, result.Value.RowId);
        Assert.Equal(1, result.Value.Tier);
    }

    [Fact]
    public void GateIn_LeavesRowAndTierEmpty_ForAVirtualBlock()
    {
        var placement = VirtualPlacement();

        var result = GateIn(placement);

        Assert.True(result.IsSuccess);
        Assert.Equal(placement.Block.Id, result.Value.BlockId);
        Assert.Null(result.Value.RowId);
        Assert.Null(result.Value.Tier);
    }

    [Fact]
    public void GateIn_TrimsVehicleNumber_AndKeepsTheDamageNote_WhenDamaged()
    {
        var details = Details(condition: StayCondition.Damaged, damageNote: " bent door ", vehicleNumber: " 51C-1 ");

        var result = GateIn(PlaceIn(NormalYard()), details);

        Assert.True(result.IsSuccess);
        Assert.Equal("51C-1", result.Value.InVehicleNumber);
        Assert.Equal("bent door", result.Value.DamageNote);
    }

    [Fact]
    public void GateIn_DropsTheDamageNote_WhenNormal()
    {
        var result = GateIn(PlaceIn(NormalYard()), Details(damageNote: "left over"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.DamageNote);
    }

    [Fact]
    public void GateIn_Fails_WhenTheContainerIsAlreadyInTheYard()
    {
        AssertError(GateIn(PlaceIn(NormalYard()), hasOpenStay: true), "Container.AlreadyInYard", ErrorType.Conflict);
    }

    [Fact]
    public void GateIn_AcceptsNow_AndRejectsTheFuture()
    {
        Assert.True(GateIn(PlaceIn(NormalYard()), Details(_now)).IsSuccess);
        AssertError(GateIn(PlaceIn(NormalYard()), Details(_now.AddSeconds(1))), "GateInAt.InFuture");
    }

    [Theory]
    [InlineData((StayGrade)3)]
    [InlineData((StayGrade)(-1))]
    public void GateIn_Fails_WhenGradeIsNotAToC(StayGrade grade)
    {
        AssertError(GateIn(PlaceIn(NormalYard()), Details(grade: grade)), "Grade.Invalid");
    }

    [Fact]
    public void GateIn_Fails_WhenConditionIsUnknown()
    {
        AssertError(GateIn(PlaceIn(NormalYard()), Details(condition: (StayCondition)5)), "Condition.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void GateIn_Fails_WhenDamagedWithoutANote(string? note)
    {
        var details = Details(condition: StayCondition.Damaged, damageNote: note);

        AssertError(GateIn(PlaceIn(NormalYard()), details), "DamageNote.Required");
    }

    [Fact]
    public void GateIn_Fails_WhenTheDamageNoteIsTooLong()
    {
        var note = new string('x', ContainerStay.DamageNoteMaxLength + 1);
        var details = Details(condition: StayCondition.Damaged, damageNote: note);

        AssertError(GateIn(PlaceIn(NormalYard()), details), "DamageNote.TooLong");
    }

    [Fact]
    public void GateIn_Fails_WhenVehicleTypeIsUnknown()
    {
        AssertError(GateIn(PlaceIn(NormalYard()), Details(vehicleType: (VehicleType)9)), "InVehicleType.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void GateIn_Fails_WhenVehicleNumberIsEmpty(string? number)
    {
        AssertError(GateIn(PlaceIn(NormalYard()), Details(vehicleNumber: number)), "InVehicleNumber.Empty");
    }

    [Fact]
    public void GateIn_Fails_WhenVehicleNumberIsTooLong()
    {
        var number = new string('1', ContainerStay.VehicleNumberMaxLength + 1);

        AssertError(GateIn(PlaceIn(NormalYard()), Details(vehicleNumber: number)), "InVehicleNumber.TooLong");
    }

    [Fact]
    public void GateIn_Fails_WhenAVirtualBlockGetsARowOrTier()
    {
        var virtualBlock = Block.Create("V1", "Virtual 1", true).Value;
        var yard = NormalYard();

        var withRow = new GateInPlacement(virtualBlock, null, yard.Row, null, []);
        var withTier = new GateInPlacement(virtualBlock, null, null, 1, []);

        AssertError(GateIn(withRow), "Position.NotAllowedInVirtualBlock");
        AssertError(GateIn(withTier), "Position.NotAllowedInVirtualBlock");
    }

    [Fact]
    public void GateIn_Fails_WhenANormalBlockHasNoRowOrTier()
    {
        var yard = NormalYard();

        AssertError(GateIn(new GateInPlacement(yard.Block, null, null, null, [])), "Position.RowAndTierRequired");
        AssertError(GateIn(PlaceIn(yard, tier: null)), "Position.RowAndTierRequired");
    }

    [Fact]
    public void GateIn_Fails_WhenTheRowBelongsToAnotherBay()
    {
        var yard = NormalYard();
        var otherBay = Bay.Create(yard.Block, 3).Value;

        var placement = new GateInPlacement(yard.Block, otherBay, yard.Row, 1, []);

        AssertError(GateIn(placement), "Position.Inconsistent");
    }

    [Fact]
    public void GateIn_Fails_WhenTheBayBelongsToAnotherBlock()
    {
        var yard = NormalYard();
        var otherBlock = Block.Create("B2", "Block B2", false).Value;

        var placement = new GateInPlacement(otherBlock, yard.Bay, yard.Row, 1, []);

        AssertError(GateIn(placement), "Position.Inconsistent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    public void GateIn_Fails_WhenTierIsOutsideTheRow(int tier)
    {
        AssertError(GateIn(PlaceIn(NormalYard(maxTier: 4), tier)), "Tier.OutOfRange");
    }

    [Fact]
    public void GateIn_AcceptsTheTopTier_WhenTheTiersBelowAreFull()
    {
        var result = GateIn(PlaceIn(NormalYard(maxTier: 4), 4, 1, 2, 3));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GateIn_Fails_WhenA20ftContainerGoesToAnEvenBay()
    {
        AssertError(GateIn(PlaceIn(NormalYard(bayNo: 2)), container: Container(20)), "Bay.SizeMismatch");
    }

    [Fact]
    public void GateIn_Fails_WhenA40ftContainerGoesToAnOddBay()
    {
        AssertError(GateIn(PlaceIn(NormalYard(bayNo: 1)), container: Container(40)), "Bay.SizeMismatch");
    }

    [Fact]
    public void GateIn_Accepts40ftContainerInAnEvenBay()
    {
        Assert.True(GateIn(PlaceIn(NormalYard(bayNo: 2)), container: Container(40)).IsSuccess);
    }

    [Fact]
    public void GateIn_Fails_WhenThePositionIsTaken()
    {
        AssertError(GateIn(PlaceIn(NormalYard(), 1, 1)), "Position.Occupied", ErrorType.Conflict);
    }

    [Fact]
    public void GateIn_Fails_WhenTheTierBelowIsEmpty()
    {
        AssertError(GateIn(PlaceIn(NormalYard(), 3, 1)), "Tier.BelowEmpty");
    }

    [Fact]
    public void GateIn_AcceptsTier2_WhenTier1IsTaken()
    {
        Assert.True(GateIn(PlaceIn(NormalYard(), 2, 1)).IsSuccess);
    }
}
