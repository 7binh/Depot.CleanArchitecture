using Depot.Application.Features.ContainerStays.GateIn;
using Depot.Application.Features.YardRows.Create;
using Depot.Domain.Gate;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.ContainerStays;

internal sealed record GateInYard(Guid ContainerId, Guid BlockId, Guid RowId);

internal static class GateInTestData
{
    public const string SecondNumber = "ABCU1234560";

    /// <summary>One 20ft container (CSQU3054383), and block A with an odd bay and a 4-tier row.</summary>
    public static async Task<GateInYard> CreateYardAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", cancellationToken);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", cancellationToken);
        var container = await ContainerTestData.CreateHandler(dbContext)
            .HandleAsync(ContainerTestData.Command(typeId), cancellationToken);

        var blockId = await YardTestData.CreateBlockAsync(dbContext, "A", cancellationToken);
        var bayId = await YardTestData.CreateBayAsync(dbContext, blockId, 1, cancellationToken);
        var row = await new CreateYardRowHandler(dbContext)
            .HandleAsync(new CreateYardRowCommand(blockId, bayId, 1, 4), cancellationToken);

        return new GateInYard(container.Value.Id, blockId, row.Value.Id);
    }

    /// <summary>A second 20ft container, owned by operator ABC.</summary>
    public static async Task<Guid> CreateSecondContainerAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await ContainerTestData.CreateOperatorAsync(dbContext, "ABC", cancellationToken);
        var typeId = dbContext.ContainerTypes.First(x => x.Code == "DRY").Id;
        var container = await ContainerTestData.CreateHandler(dbContext)
            .HandleAsync(ContainerTestData.Command(typeId, SecondNumber), cancellationToken);
        return container.Value.Id;
    }

    public static GateInCommand Command(
        Guid containerId,
        Guid blockId,
        Guid? rowId,
        int? tier = 1,
        DateTimeOffset? gateInAt = null)
    {
        return new GateInCommand(
            containerId,
            gateInAt,
            StayGrade.A,
            StayCondition.Normal,
            null,
            VehicleType.Truck,
            "51C-12345",
            blockId,
            rowId,
            tier);
    }

    public static GateInHandler CreateHandler(AppDbContext dbContext)
    {
        return new GateInHandler(dbContext, ContainerTestData.Clock);
    }
}
