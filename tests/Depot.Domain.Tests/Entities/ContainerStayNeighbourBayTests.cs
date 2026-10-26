using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

// A 40ft container in an even bay N also covers the odd bays N-1 and N+1 (BR-YD-04).
public sealed class ContainerStayNeighbourBayTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static readonly int[] _none = [];

    private static Container Container(int sizeFeet)
    {
        var spec = new ContainerSpec("22G1", sizeFeet, 30480, 2200, new DateOnly(2020, 5, 1));
        var lineOperator = LineOperator.Create("CSQ", "Operator").Value;
        return Domain.Containers.Container.Create("CSQU3054383", lineOperator, Guid.NewGuid(), spec, new DateOnly(2026, 10, 26)).Value;
    }

    private static Result<ContainerStay> GateIn(int bayNo, int sizeFeet, int tier, int[] occupiedHere, NeighbourTiers neighbours)
    {
        var block = Block.Create("A1", "Block A1", false).Value;
        var bay = Bay.Create(block, bayNo).Value;
        var row = YardRow.Create(bay.Id, 1, 4).Value;
        var details = new GateInDetails(_now, StayGrade.A, StayCondition.Normal, null, new Vehicle(VehicleType.Truck, "51C-1"));
        var placement = new GateInPlacement(block, bay, row, tier, occupiedHere, neighbours);

        return ContainerStay.GateIn(Container(sizeFeet), false, details, placement, _now);
    }

    private static NeighbourTiers Around(
        int[]? before = null,
        int[]? after = null,
        int[]? farBefore = null,
        int[]? farAfter = null,
        bool beforeExists = true,
        bool afterExists = true)
    {
        return new NeighbourTiers(
            before ?? _none,
            after ?? _none,
            farBefore ?? _none,
            farAfter ?? _none,
            beforeExists,
            afterExists);
    }

    private static void AssertError(Result<ContainerStay> result, string code, ErrorType type)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

    [Fact]
    public void FortyFoot_Fails_WhenAnOddBayBesideItHoldsAContainerOnTheSameTier()
    {
        AssertError(GateIn(2, 40, 1, _none, Around(before: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
        AssertError(GateIn(2, 40, 1, _none, Around(after: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
    }

    [Fact]
    public void FortyFoot_Fails_WhenTheEvenBayTwoAwayShareAnOddBay()
    {
        AssertError(GateIn(4, 40, 1, _none, Around(farBefore: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
        AssertError(GateIn(4, 40, 1, _none, Around(farAfter: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
    }

    [Fact]
    public void FortyFoot_IsAccepted_WhenTheNeighboursHoldOtherTiersOnly()
    {
        Assert.True(GateIn(2, 40, 1, _none, Around(before: [2], after: [3])).IsSuccess);
    }

    [Fact]
    public void TwentyFoot_Fails_WhenAnEvenBayBesideItCoversTheSameTier()
    {
        AssertError(GateIn(3, 20, 1, _none, Around(before: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
        AssertError(GateIn(3, 20, 1, _none, Around(after: [1])), "Position.OverlapsNeighbour", ErrorType.Conflict);
    }

    [Fact]
    public void TwentyFoot_IgnoresTheOddBaysTwoAway()
    {
        Assert.True(GateIn(3, 20, 1, _none, Around(farBefore: [1], farAfter: [1])).IsSuccess);
    }

    [Fact]
    public void FortyFoot_CanRestOnTwoTwentyFootContainers_OneInEachOddBay()
    {
        Assert.True(GateIn(2, 40, 2, _none, Around(before: [1], after: [1])).IsSuccess);
    }

    [Fact]
    public void FortyFoot_Fails_WhenOnlyOneTwentyFootContainerIsUnderIt()
    {
        AssertError(GateIn(2, 40, 2, _none, Around(before: [1])), "Tier.BelowEmpty", ErrorType.Validation);
        AssertError(GateIn(2, 40, 2, _none, Around(after: [1])), "Tier.BelowEmpty", ErrorType.Validation);
    }

    [Fact]
    public void FortyFoot_CanRestOnAFortyFootContainerInTheSameBay()
    {
        Assert.True(GateIn(2, 40, 2, [1], Around()).IsSuccess);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void FortyFoot_Fails_WhenAnOddBayBesideItIsNotDeclared(bool beforeExists, bool afterExists)
    {
        var neighbours = Around(beforeExists: beforeExists, afterExists: afterExists);

        AssertError(GateIn(2, 40, 1, _none, neighbours), "Bay.NeighboursMissing", ErrorType.Validation);
    }

    [Fact]
    public void TwentyFoot_DoesNotNeedItsNeighbourBays()
    {
        Assert.True(GateIn(1, 20, 1, _none, Around(beforeExists: false, afterExists: false)).IsSuccess);
    }

    [Fact]
    public void TwentyFoot_Fails_WhenItWouldRestOnAFortyFootContainer()
    {
        AssertError(GateIn(1, 20, 2, _none, Around(after: [1])), "Tier.BelowEmpty", ErrorType.Validation);
    }
}
