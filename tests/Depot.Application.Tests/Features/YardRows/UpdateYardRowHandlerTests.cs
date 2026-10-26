using Depot.Application.Features.YardRows;
using Depot.Application.Features.YardRows.Update;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Domain.Containers.Rules;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.YardRows;

public sealed class UpdateYardRowHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private sealed record Yard(Guid BlockId, Guid BayId, Guid RowId, Guid ContainerId);

    // One 20ft container, and block A with bay 1 and a 4-tier row.
    private static async Task<Yard> CreateYardAsync(AppDbContext dbContext)
    {
        var yard = await GateInTestData.CreateYardAsync(dbContext, _ct);
        var bay = await dbContext.Bays.SingleAsync(_ct);
        return new Yard(yard.BlockId, bay.Id, yard.RowId, yard.ContainerId);
    }

    private static Task<Result<YardRowResponse>> UpdateAsync(AppDbContext dbContext, Yard yard, int maxTier)
    {
        return new UpdateYardRowHandler(dbContext)
            .HandleAsync(new UpdateYardRowCommand(yard.BlockId, yard.BayId, yard.RowId, maxTier), _ct);
    }

    private static async Task GateInAtTierAsync(AppDbContext dbContext, Yard yard, Guid containerId, int tier)
    {
        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(containerId, yard.BlockId, yard.RowId, tier), _ct);
        Assert.True(result.IsSuccess);
    }

    private static async Task<Guid> CreateContainerAsync(AppDbContext dbContext, int serial)
    {
        var prefix = $"CSQU30541{serial}";
        var number = prefix + ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix);
        var typeId = await dbContext.ContainerTypes.Select(x => x.Id).SingleAsync(_ct);
        var container = await ContainerTestData.CreateHandler(dbContext)
            .HandleAsync(ContainerTestData.Command(typeId, number), _ct);
        return container.Value.Id;
    }

    [Fact]
    public async Task HandleAsync_ChangesTheMaxTier_OfAnEmptyRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);

        var result = await UpdateAsync(dbContext, yard, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MaxTier);
        Assert.Equal(2, (await dbContext.YardRows.SingleAsync(_ct)).MaxTier);
    }

    [Fact]
    public async Task HandleAsync_RefusesToLowerTheRow_WhileAContainerStandsAboveTheNewMaximum()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        await GateInAtTierAsync(dbContext, yard, yard.ContainerId, 1);
        await GateInAtTierAsync(dbContext, yard, await CreateContainerAsync(dbContext, 1), 2);
        await GateInAtTierAsync(dbContext, yard, await CreateContainerAsync(dbContext, 2), 3);

        var refused = await UpdateAsync(dbContext, yard, 2);
        var allowed = await UpdateAsync(dbContext, yard, 3);

        Assert.True(refused.IsFailure);
        Assert.Equal("MaxTier.BelowOccupied", refused.Error.Code);
        Assert.Equal(ErrorType.Conflict, refused.Error.Type);
        Assert.True(allowed.IsSuccess);
        Assert.Equal(3, (await dbContext.YardRows.SingleAsync(_ct)).MaxTier);
    }

    [Fact]
    public async Task HandleAsync_IgnoresContainersThatAlreadyLeft()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        await GateInAtTierAsync(dbContext, yard, yard.ContainerId, 1);
        await GateInAtTierAsync(dbContext, yard, await CreateContainerAsync(dbContext, 1), 2);
        var top = await CreateContainerAsync(dbContext, 2);
        await GateInAtTierAsync(dbContext, yard, top, 3);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        var left = await GateOutTestData.CreateHandler(dbContext)
            .HandleAsync(GateOutTestData.Command(top, orderId), _ct);
        Assert.True(left.IsSuccess);

        var result = await UpdateAsync(dbContext, yard, 2);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public async Task HandleAsync_ReturnsValidationError_WhenTheMaxTierIsOutOfRange(int maxTier)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);

        var result = await UpdateAsync(dbContext, yard, maxTier);

        Assert.True(result.IsFailure);
        Assert.Equal("MaxTier.Invalid", result.Error.Code);
        Assert.Equal(4, (await dbContext.YardRows.SingleAsync(_ct)).MaxTier);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheRowOrBayDoesNotBelongTogether()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await CreateYardAsync(dbContext);
        var handler = new UpdateYardRowHandler(dbContext);

        var wrongRow = await handler.HandleAsync(new UpdateYardRowCommand(yard.BlockId, yard.BayId, Guid.NewGuid(), 3), _ct);
        var wrongBay = await handler.HandleAsync(new UpdateYardRowCommand(yard.BlockId, Guid.NewGuid(), yard.RowId, 3), _ct);

        Assert.True(wrongRow.IsFailure);
        Assert.True(wrongBay.IsFailure);
        Assert.Equal("YardRow.NotFound", wrongRow.Error.Code);
        Assert.Equal("Bay.NotFound", wrongBay.Error.Code);
    }
}
