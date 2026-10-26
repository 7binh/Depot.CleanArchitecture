using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class ContainerStayGateOutTests
{
    private static readonly DateTimeOffset _gateIn = new(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = new(2026, 10, 26);

    private readonly LineOperator _lineOperator = LineOperator.Create("CSQ", "Operator").Value;
    private readonly Guid _typeId = Guid.NewGuid();

    private Container Container(int sizeFeet = 20)
    {
        var spec = new ContainerSpec("22G1", sizeFeet, 30480, 2200, new DateOnly(2020, 5, 1));
        return Domain.Containers.Container.Create("CSQU3054383", _lineOperator, _typeId, spec, _today).Value;
    }

    private static ContainerStay OpenStay(Container container)
    {
        var block = Block.Create("V1", "Virtual 1", true).Value;
        var details = new GateInDetails(
            _gateIn,
            StayGrade.A,
            StayCondition.Normal,
            null,
            new Vehicle(VehicleType.Truck, "51C-1"));
        var placement = new GateInPlacement(block, null, null, null, []);

        return ContainerStay.GateIn(container, false, details, placement, _now).Value;
    }

    private ReleaseOrder Order(
        int quantity = 2,
        DateOnly? expiry = null,
        Guid? lineOperatorId = null,
        int sizeFeet = 20)
    {
        var lines = new[] { new ReleaseOrderLine(_typeId, sizeFeet, quantity) };
        return ReleaseOrder.Create(
            lineOperatorId ?? _lineOperator.Id,
            Guid.NewGuid(),
            "RO-001",
            new DateOnly(2026, 10, 1),
            expiry ?? new DateOnly(2026, 11, 1),
            "V1",
            lines).Value;
    }

    private static GateOutDetails Out(DateTimeOffset? at = null, VehicleType type = VehicleType.Truck, string? number = "51C-99")
    {
        return new GateOutDetails(at ?? _now, new Vehicle(type, number));
    }

    private static Result GateOut(
        ContainerStay stay,
        Container container,
        ReleaseOrder order,
        int delivered = 0,
        GateOutDetails? details = null,
        DateOnly? gateOutDate = null)
    {
        return stay.GateOut(container, new GateOutRelease(order, delivered), details ?? Out(), _now, gateOutDate ?? _today);
    }

    private static void AssertError(Result result, string code, ErrorType type = ErrorType.Validation)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

    [Fact]
    public void GateOut_ClosesTheStay_AndRecordsTheLineAndVehicle()
    {
        var container = Container();
        var stay = OpenStay(container);
        var order = Order();

        var result = GateOut(stay, container, order, details: Out(number: " 51C-99 "));

        Assert.True(result.IsSuccess);
        Assert.False(stay.IsOpen);
        Assert.Equal(_now, stay.GateOutAt);
        Assert.Equal(VehicleType.Truck, stay.OutVehicleType);
        Assert.Equal("51C-99", stay.OutVehicleNumber);
        Assert.Equal(order.Items.Single().Id, stay.ReleaseOrderItemId);
    }

    [Fact]
    public void GateOut_LeavesTheStayOpenAndUnchanged_WhenARuleFails()
    {
        var container = Container();
        var stay = OpenStay(container);

        var result = GateOut(stay, container, Order(lineOperatorId: Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.True(stay.IsOpen);
        Assert.Null(stay.GateOutAt);
        Assert.Null(stay.OutVehicleNumber);
        Assert.Null(stay.ReleaseOrderItemId);
    }

    [Fact]
    public void GateOut_Fails_WhenTheStayIsAlreadyClosed()
    {
        var container = Container();
        var stay = OpenStay(container);
        GateOut(stay, container, Order());

        AssertError(GateOut(stay, container, Order()), "Stay.AlreadyClosed", ErrorType.Conflict);
    }

    [Fact]
    public void GateOut_AcceptsLeavingAtTheGateInTime_AndRejectsEarlier()
    {
        var container = Container();

        Assert.True(GateOut(OpenStay(container), container, Order(), details: Out(_gateIn)).IsSuccess);
        AssertError(
            GateOut(OpenStay(container), container, Order(), details: Out(_gateIn.AddSeconds(-1))),
            "GateOutAt.BeforeGateIn");
    }

    [Fact]
    public void GateOut_Fails_WhenTheTimeIsInTheFuture()
    {
        var container = Container();

        AssertError(
            GateOut(OpenStay(container), container, Order(), details: Out(_now.AddSeconds(1))),
            "GateOutAt.InFuture");
    }

    [Fact]
    public void GateOut_Fails_WhenTheVehicleTypeIsUnknown()
    {
        var container = Container();

        AssertError(
            GateOut(OpenStay(container), container, Order(), details: Out(type: (VehicleType)9)),
            "OutVehicleType.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void GateOut_Fails_WhenTheVehicleNumberIsEmpty(string? number)
    {
        var container = Container();

        AssertError(
            GateOut(OpenStay(container), container, Order(), details: Out(number: number)),
            "OutVehicleNumber.Empty");
    }

    [Fact]
    public void GateOut_Fails_WhenTheVehicleNumberIsTooLong()
    {
        var container = Container();
        var number = new string('1', ContainerStay.VehicleNumberMaxLength + 1);

        AssertError(
            GateOut(OpenStay(container), container, Order(), details: Out(number: number)),
            "OutVehicleNumber.TooLong");
    }

    [Fact]
    public void GateOut_Fails_WhenTheOrderIsFromAnotherLineOperator()
    {
        var container = Container();

        AssertError(
            GateOut(OpenStay(container), container, Order(lineOperatorId: Guid.NewGuid())),
            "ReleaseOrder.OperatorMismatch");
    }

    [Fact]
    public void GateOut_AcceptsTheExpiryDay_AndRejectsTheDayAfter()
    {
        var container = Container();
        var order = Order(expiry: _today);

        Assert.True(GateOut(OpenStay(container), container, order).IsSuccess);
        AssertError(
            GateOut(OpenStay(container), container, order, gateOutDate: _today.AddDays(1)),
            "ReleaseOrder.Expired",
            ErrorType.Conflict);
    }

    [Fact]
    public void GateOut_Fails_WhenTheOrderHasNoLineForTheContainerSize()
    {
        var container = Container(20);

        AssertError(
            GateOut(OpenStay(container), container, Order(sizeFeet: 40)),
            "ReleaseOrder.NoMatchingLine");
    }

    [Fact]
    public void GateOut_Fails_WhenTheLineHasNothingLeft()
    {
        var container = Container();
        var order = Order(quantity: 2);

        Assert.True(GateOut(OpenStay(container), container, order, delivered: 1).IsSuccess);
        AssertError(
            GateOut(OpenStay(container), container, order, delivered: 2),
            "ReleaseOrder.LineFull",
            ErrorType.Conflict);
    }

    [Fact]
    public void FindItemFor_ReturnsTheLineOfTheSameTypeAndSize_OrNull()
    {
        var container = Container(20);
        var order = Order(sizeFeet: 20);

        Assert.Same(order.Items.Single(), order.FindItemFor(container));
        Assert.Null(Order(sizeFeet: 40).FindItemFor(container));
    }
}
