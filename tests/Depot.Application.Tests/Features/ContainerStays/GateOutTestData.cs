using Depot.Application.Features.ContainerStays.GateOut;
using Depot.Application.Features.Customers.Create;
using Depot.Application.Features.ReleaseOrders.Create;
using Depot.Application.Tests.Features.ReleaseOrders;
using Depot.Domain.Containers.Rules;
using Depot.Domain.Gate;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests.Features.ContainerStays;

internal static class GateOutTestData
{
    /// <summary>Gates the yard's container in at tier 1 and returns the yard.</summary>
    public static async Task<GateInYard> CreateYardWithContainerInAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var yard = await GateInTestData.CreateYardAsync(dbContext, cancellationToken);
        await GateInTestData.CreateHandler(dbContext)
            .HandleAsync(GateInTestData.Command(yard.ContainerId, yard.BlockId, yard.RowId), cancellationToken);
        return yard;
    }

    /// <summary>A second 20ft container of operator CSQ, with a valid check digit.</summary>
    public static async Task<Guid> CreateSecondCsqContainerAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string prefix = "CSQU305439";
        var number = prefix + ContainerNumberCheckDigitRule.ComputeCheckDigit(prefix);
        var typeId = await dbContext.ContainerTypes.Where(x => x.Code == "DRY").Select(x => x.Id).SingleAsync(cancellationToken);
        var container = await ContainerTestData.CreateHandler(dbContext)
            .HandleAsync(ContainerTestData.Command(typeId, number), cancellationToken);
        return container.Value.Id;
    }

    /// <summary>A release order of operator CSQ for 20ft dry containers.</summary>
    public static async Task<Guid> CreateOrderAsync(
        AppDbContext dbContext,
        int quantity,
        CancellationToken cancellationToken,
        int expiresInDays = 10,
        Guid? operatorId = null,
        string orderNo = "RO-001")
    {
        var customerId = await dbContext.Customers.Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken)
            ?? (await new CreateCustomerHandler(dbContext)
                .HandleAsync(new CreateCustomerCommand("0101234567", "Cong ty ABC"), cancellationToken)).Value.Id;
        var refs = new ReleaseOrderRefs(
            operatorId ?? await dbContext.LineOperators.Where(x => x.Code == "CSQ").Select(x => x.Id).SingleAsync(cancellationToken),
            customerId,
            await dbContext.ContainerTypes.Where(x => x.Code == "DRY").Select(x => x.Id).SingleAsync(cancellationToken));

        var command = ReleaseOrderTestData.Command(
            refs,
            orderNo,
            lines: [new ReleaseOrderLineRequest(refs.TypeId, 20, quantity)]) with
        {
            IssuedDate = ContainerTestData.Today.AddDays(-20),
            ExpiryDate = ContainerTestData.Today.AddDays(expiresInDays)
        };
        var order = await ReleaseOrderTestData.CreateHandler(dbContext).HandleAsync(command, cancellationToken);
        return order.Value.Id;
    }

    public static GateOutCommand Command(
        Guid containerId,
        Guid releaseOrderId,
        DateTimeOffset? gateOutAt = null)
    {
        return new GateOutCommand(containerId, releaseOrderId, gateOutAt, VehicleType.Truck, "51C-99999");
    }

    public static GateOutHandler CreateHandler(AppDbContext dbContext)
    {
        return new GateOutHandler(dbContext, ContainerTestData.Clock);
    }
}
