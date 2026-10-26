using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.ReleaseOrders;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ReleaseOrders.Create;

public sealed class CreateReleaseOrderHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateReleaseOrderCommand, Result<ReleaseOrderResponse>>
{
    public async Task<Result<ReleaseOrderResponse>> HandleAsync(
        CreateReleaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var references = await CheckReferencesAsync(command, cancellationToken);
        if (references.IsFailure)
        {
            return Result.Failure<ReleaseOrderResponse>(references.Error);
        }

        var created = CreateOrder(command);
        if (created.IsFailure)
        {
            return Result.Failure<ReleaseOrderResponse>(created.Error);
        }

        var order = created.Value;

        if (await OrderNoExistsAsync(order, cancellationToken))
        {
            return Result.Failure<ReleaseOrderResponse>(ReleaseOrderErrors.OrderNoExists(order.OrderNo));
        }

        return await SaveAsync(order, cancellationToken);
    }

    private async Task<Result<ReleaseOrderResponse>> SaveAsync(ReleaseOrder order, CancellationToken cancellationToken)
    {
        dbContext.ReleaseOrders.Add(order);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await OrderNoExistsAsync(order, cancellationToken))
            {
                throw;
            }

            return Result.Failure<ReleaseOrderResponse>(ReleaseOrderErrors.OrderNoExists(order.OrderNo));
        }

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        return Result.Success(ReleaseOrderResponse.From(order, today, new Dictionary<Guid, int>()));
    }

    private async Task<Result> CheckReferencesAsync(
        CreateReleaseOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.LineOperators.AnyAsync(x => x.Id == command.LineOperatorId, cancellationToken))
        {
            return Result.Failure(ReleaseOrderErrors.OperatorNotFound(command.LineOperatorId));
        }

        if (!await dbContext.Customers.AnyAsync(x => x.Id == command.CustomerId, cancellationToken))
        {
            return Result.Failure(ReleaseOrderErrors.CustomerNotFound(command.CustomerId));
        }

        return await CheckContainerTypesAsync(command.Lines, cancellationToken);
    }

    private async Task<Result> CheckContainerTypesAsync(
        IReadOnlyList<ReleaseOrderLineRequest>? lines,
        CancellationToken cancellationToken)
    {
        var requested = (lines ?? []).Select(x => x.ContainerTypeId).Distinct().ToList();
        var existing = await dbContext.ContainerTypes
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missing = requested.Except(existing).ToList();
        return missing.Count == 0
            ? Result.Success()
            : Result.Failure(ReleaseOrderErrors.ContainerTypeNotFound(missing[0]));
    }

    private static Result<ReleaseOrder> CreateOrder(CreateReleaseOrderCommand command)
    {
        var lines = (command.Lines ?? [])
            .Select(x => new ReleaseOrderLine(x.ContainerTypeId, x.SizeFeet, x.Quantity))
            .ToList();

        return ReleaseOrder.Create(
            command.LineOperatorId,
            command.CustomerId,
            command.OrderNo,
            command.IssuedDate,
            command.ExpiryDate,
            command.ExportVessel,
            lines);
    }

    private Task<bool> OrderNoExistsAsync(ReleaseOrder order, CancellationToken cancellationToken)
    {
        return dbContext.ReleaseOrders.AnyAsync(
            x => x.LineOperatorId == order.LineOperatorId && x.OrderNo == order.OrderNo,
            cancellationToken);
    }
}
