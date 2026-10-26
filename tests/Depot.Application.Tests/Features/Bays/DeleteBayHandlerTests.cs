using Depot.Application.Features.Bays.Delete;
using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GenerateLayout;
using Depot.Application.Tests.Features.ContainerStays;
using Depot.Domain.Common;
using Depot.Domain.Containers.Rules;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.Bays;

public sealed class DeleteBayHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private sealed record Layout(Guid BlockId, Guid[] BayIds);

    // Block A with bays 1, 2 and 3, each holding two rows. BayIds[0] is bay 1.
    private static async Task<Layout> CreateLayoutAsync(AppDbContext dbContext)
    {
        var blockId = (await new CreateBlockHandler(dbContext).HandleAsync(new CreateBlockCommand("A", "Khu A", false), _ct)).Value.Id;
        await new GenerateLayoutHandler(dbContext).HandleAsync(new GenerateLayoutCommand(blockId, 1, 3, 2, 4), _ct);
        var bayIds = await dbContext.Bays.OrderBy(x => x.BayNo).Select(x => x.Id).ToArrayAsync(_ct);
        return new Layout(blockId, bayIds);
    }

    private static Task<Result> DeleteAsync(AppDbContext dbContext, Layout layout, int bayNo)
    {
        return new DeleteBayHandler(dbContext).HandleAsync(new DeleteBayCommand(layout.BlockId, layout.BayIds[bayNo - 1]), _ct);
    }

    private static async Task<Guid> CreateContainerAsync(AppDbContext dbContext, int sizeFeet, int serial)
    {
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", _ct);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", _ct);
        var prefix = $"CSQU30542{serial}";
        var number = prefix + ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix);
        var command = ContainerTestData.Command(typeId, number) with { SizeFeet = sizeFeet };
        return (await ContainerTestData.CreateHandler(dbContext).HandleAsync(command, _ct)).Value.Id;
    }

    private static async Task GateInAsync(AppDbContext dbContext, Layout layout, Guid containerId, int bayNo)
    {
        var rowId = await dbContext.YardRows.Where(x => x.BayId == layout.BayIds[bayNo - 1] && x.RowNo == 1)
            .Select(x => x.Id).SingleAsync(_ct);
        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(containerId, layout.BlockId, rowId, 1), _ct);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_RemovesTheBayWithItsRows_AndKeepsTheOthers()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var layout = await CreateLayoutAsync(dbContext);

        var result = await DeleteAsync(dbContext, layout, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 3], await dbContext.Bays.OrderBy(x => x.BayNo).Select(x => x.BayNo).ToListAsync(_ct));
        Assert.Equal(4, await dbContext.YardRows.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenTheBayIsNotInThatBlock()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var layout = await CreateLayoutAsync(dbContext);

        var result = await new DeleteBayHandler(dbContext)
            .HandleAsync(new DeleteBayCommand(Guid.NewGuid(), layout.BayIds[0]), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NotFound", result.Error.Code);
        Assert.Equal(3, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_RefusesABayThatHoldsAContainer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var layout = await CreateLayoutAsync(dbContext);
        await GateInAsync(dbContext, layout, await CreateContainerAsync(dbContext, 20, 1), 1);

        var result = await DeleteAsync(dbContext, layout, 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.HasContainers", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(3, await dbContext.Bays.CountAsync(_ct));
    }

    [Fact]
    public async Task HandleAsync_RefusesAnOddBay_WhileAFortyFootContainerLiesOverIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var layout = await CreateLayoutAsync(dbContext);
        await GateInAsync(dbContext, layout, await CreateContainerAsync(dbContext, 40, 1), 2);

        var result = await DeleteAsync(dbContext, layout, 3);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.HasContainers", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ForgetsThePositionOfContainersThatAlreadyLeft_AndKeepsTheirHistory()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var yard = await GateOutTestData.CreateYardWithContainerInAsync(dbContext, _ct);
        var orderId = await GateOutTestData.CreateOrderAsync(dbContext, 1, _ct);
        await GateOutTestData.CreateHandler(dbContext).HandleAsync(GateOutTestData.Command(yard.ContainerId, orderId), _ct);
        var bayId = await dbContext.Bays.Select(x => x.Id).SingleAsync(_ct);

        var result = await new DeleteBayHandler(dbContext).HandleAsync(new DeleteBayCommand(yard.BlockId, bayId), _ct);

        Assert.True(result.IsSuccess);
        var stay = await dbContext.ContainerStays.SingleAsync(_ct);
        Assert.Null(stay.RowId);
        Assert.Equal(yard.BlockId, stay.BlockId);
        Assert.Equal(yard.ContainerId, stay.ContainerId);
    }

    [Fact]
    public async Task GateIn_RefusesAFortyFootContainer_WhenAnOddBayBesideItWasRemoved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var layout = await CreateLayoutAsync(dbContext);
        await DeleteAsync(dbContext, layout, 3);
        var forty = await CreateContainerAsync(dbContext, 40, 1);
        var rowId = await dbContext.YardRows.Where(x => x.BayId == layout.BayIds[1] && x.RowNo == 1)
            .Select(x => x.Id).SingleAsync(_ct);

        var result = await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(forty, layout.BlockId, rowId, 1), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.NeighboursMissing", result.Error.Code);
    }
}
