using Depot.Domain.Common;
using Depot.Domain.ReleaseOrders;

namespace Depot.Domain.Tests.Entities;

public sealed class ReleaseOrderTests
{
    private static readonly DateOnly _issued = new(2026, 10, 20);
    private static readonly DateOnly _expiry = new(2026, 10, 30);

    private static ReleaseOrderLine Line(int sizeFeet = 20, int quantity = 5, Guid? typeId = null)
    {
        return new ReleaseOrderLine(typeId ?? Guid.NewGuid(), sizeFeet, quantity);
    }

    private static Result<ReleaseOrder> Create(
        string? orderNo = "RO-001",
        DateOnly? expiry = null,
        string? vessel = "EVER GIVEN V.12",
        IReadOnlyCollection<ReleaseOrderLine>? lines = null)
    {
        var effectiveLines = lines ?? [Line()];
        return ReleaseOrder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            orderNo,
            _issued,
            expiry ?? _expiry,
            vessel,
            effectiveLines);
    }

    private static void AssertValidationError(Result<ReleaseOrder> result, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_StoresHeaderAndLines_AndTrimsText()
    {
        var typeId = Guid.NewGuid();

        var result = Create("  RO-001 ", vessel: " EVER GIVEN V.12 ", lines: [Line(40, 3, typeId), Line(20, 2, typeId)]);

        Assert.True(result.IsSuccess);
        Assert.Equal("RO-001", result.Value.OrderNo);
        Assert.Equal("EVER GIVEN V.12", result.Value.ExportVessel);
        Assert.Equal(_issued, result.Value.IssuedDate);
        Assert.Equal(_expiry, result.Value.ExpiryDate);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(result.Value.Id, item.ReleaseOrderId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_Fails_WhenOrderNoIsEmpty(string? orderNo)
    {
        AssertValidationError(Create(orderNo), "OrderNo.Empty");
    }

    [Fact]
    public void Create_Fails_WhenOrderNoIsTooLong()
    {
        AssertValidationError(Create(new string('A', ReleaseOrder.OrderNoMaxLength + 1)), "OrderNo.TooLong");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Fails_WhenExportVesselIsEmpty(string? vessel)
    {
        AssertValidationError(Create(vessel: vessel), "ExportVessel.Empty");
    }

    [Fact]
    public void Create_AcceptsExpiryOnTheIssuedDate()
    {
        Assert.True(Create(expiry: _issued).IsSuccess);
    }

    [Fact]
    public void Create_Fails_WhenExpiryIsBeforeIssuedDate()
    {
        AssertValidationError(Create(expiry: _issued.AddDays(-1)), "ExpiryDate.BeforeIssuedDate");
    }

    [Fact]
    public void Create_Fails_WhenThereAreNoLines()
    {
        var result = ReleaseOrder.Create(Guid.NewGuid(), Guid.NewGuid(), "RO-001", _issued, _expiry, "V1", []);

        AssertValidationError(result, "Lines.Empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Fails_WhenQuantityIsNotPositive(int quantity)
    {
        AssertValidationError(Create(lines: [Line(quantity: quantity)]), "Quantity.Invalid");
    }

    [Fact]
    public void Create_Fails_WhenSizeIsNot20Or40()
    {
        AssertValidationError(Create(lines: [Line(sizeFeet: 45)]), "SizeFeet.Invalid");
    }

    [Fact]
    public void Create_Fails_WhenTypeAndSizeAppearTwice()
    {
        var typeId = Guid.NewGuid();

        AssertValidationError(Create(lines: [Line(20, 1, typeId), Line(20, 2, typeId)]), "Lines.Duplicate");
    }

    [Fact]
    public void Create_AcceptsSameTypeWithDifferentSizes()
    {
        var typeId = Guid.NewGuid();

        Assert.True(Create(lines: [Line(20, 1, typeId), Line(40, 2, typeId)]).IsSuccess);
    }

    [Theory]
    [InlineData(30, false)]
    [InlineData(31, true)]
    public void IsExpiredOn_IsTrueOnlyAfterTheExpiryDate(int day, bool expected)
    {
        var order = Create(expiry: new DateOnly(2026, 10, 30)).Value;

        Assert.Equal(expected, order.IsExpiredOn(new DateOnly(2026, 10, day)));
    }
}
