using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class ContainerStayMoveTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private sealed record Yard(Block Block, Bay Bay, YardRow Row);

    private static Container Container()
    {
        var spec = new ContainerSpec("22G1", 20, 30480, 2200, new DateOnly(2020, 5, 1));
        var lineOperator = LineOperator.Create("CSQ", "Operator").Value;
        return Domain.Containers.Container.Create("CSQU3054383", lineOperator, Guid.NewGuid(), spec, new DateOnly(2026, 10, 26)).Value;
    }

    private static Yard YardWithBay(Block block, int bayNo)
    {
        var bay = Bay.Create(block, bayNo).Value;
        return new Yard(block, bay, YardRow.Create(bay.Id, 1, 4).Value);
    }

    private static GateInPlacement PlaceIn(Yard yard, int? tier = 1, params int[] occupiedTiers)
    {
        return new GateInPlacement(yard.Block, yard.Bay, yard.Row, tier, occupiedTiers);
    }

    private static ContainerStay StayAt(Container container, GateInPlacement placement)
    {
        var details = new GateInDetails(_now, StayGrade.A, StayCondition.Normal, null, new Vehicle(VehicleType.Truck, "51C-12345"));
        return ContainerStay.GateIn(container, false, details, placement, _now).Value;
    }

    [Fact]
    public void MoveTo_ChangesThePosition_AndKeepsTheRestOfTheStay()
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var from = YardWithBay(block, 1);
        var to = YardWithBay(block, 3);
        var container = Container();
        var stay = StayAt(container, PlaceIn(from));

        var result = stay.MoveTo(container, PlaceIn(to, 2, 1), false);

        Assert.True(result.IsSuccess);
        Assert.Equal(to.Row.Id, stay.RowId);
        Assert.Equal(2, stay.Tier);
        Assert.Equal(_now, stay.GateInAt);
        Assert.True(stay.IsOpen);
    }

    [Fact]
    public void MoveTo_IsRefused_WhenAContainerRestsOnIt_AndKeepsThePosition()
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var from = YardWithBay(block, 1);
        var to = YardWithBay(block, 3);
        var container = Container();
        var stay = StayAt(container, PlaceIn(from));

        var result = stay.MoveTo(container, PlaceIn(to), true);

        Assert.True(result.IsFailure);
        Assert.Equal("Stay.HasContainerAbove", result.Error.Code);
        Assert.Equal(from.Row.Id, stay.RowId);
    }

    [Fact]
    public void MoveTo_IsRefused_WhenTheTargetPositionIsTaken()
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var from = YardWithBay(block, 1);
        var to = YardWithBay(block, 3);
        var container = Container();
        var stay = StayAt(container, PlaceIn(from));

        var result = stay.MoveTo(container, PlaceIn(to, 1, 1), false);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Occupied", result.Error.Code);
    }

    [Fact]
    public void MoveTo_IsRefused_WhenTheContainerWouldStayWhereItIs()
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var from = YardWithBay(block, 1);
        var container = Container();
        var stay = StayAt(container, PlaceIn(from));

        var result = stay.MoveTo(container, PlaceIn(from), false);

        Assert.True(result.IsFailure);
        Assert.Equal("Position.Unchanged", result.Error.Code);
    }

    [Fact]
    public void MoveTo_ClearsRowAndTier_WhenTheTargetIsAVirtualBlock()
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var from = YardWithBay(block, 1);
        var virtualBlock = Block.Create("V1", "Virtual 1", true).Value;
        var container = Container();
        var stay = StayAt(container, PlaceIn(from));

        var result = stay.MoveTo(container, new GateInPlacement(virtualBlock, null, null, null, []), false);

        Assert.True(result.IsSuccess);
        Assert.Equal(virtualBlock.Id, stay.BlockId);
        Assert.Null(stay.RowId);
        Assert.Null(stay.Tier);
    }
}
