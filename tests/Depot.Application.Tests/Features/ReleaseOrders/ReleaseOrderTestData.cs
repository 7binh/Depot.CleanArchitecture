using Depot.Application.Features.Customers.Create;
using Depot.Application.Features.ReleaseOrders.Create;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Features.ReleaseOrders;

internal sealed record ReleaseOrderRefs(Guid OperatorId, Guid CustomerId, Guid TypeId);

internal static class ReleaseOrderTestData
{
    public static async Task<ReleaseOrderRefs> CreateRefsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var operatorId = await ContainerTestData.CreateOperatorAsync(dbContext, "CSQ", cancellationToken);
        var typeId = await ContainerTestData.CreateTypeAsync(dbContext, "DRY", cancellationToken);
        var customer = await new CreateCustomerHandler(dbContext)
            .HandleAsync(new CreateCustomerCommand("0101234567", "Cong ty ABC"), cancellationToken);

        return new ReleaseOrderRefs(operatorId, customer.Value.Id, typeId);
    }

    public static CreateReleaseOrderCommand Command(
        ReleaseOrderRefs refs,
        string? orderNo = "RO-001",
        DateOnly? expiryDate = null,
        IReadOnlyList<ReleaseOrderLineRequest>? lines = null)
    {
        return new CreateReleaseOrderCommand(
            refs.OperatorId,
            refs.CustomerId,
            orderNo,
            ContainerTestData.Today,
            expiryDate ?? ContainerTestData.Today.AddDays(10),
            "EVER GIVEN V.12",
            lines ?? [new ReleaseOrderLineRequest(refs.TypeId, 20, 5)]);
    }

    public static CreateReleaseOrderHandler CreateHandler(AppDbContext dbContext)
    {
        return new CreateReleaseOrderHandler(dbContext, ContainerTestData.Clock);
    }
}
