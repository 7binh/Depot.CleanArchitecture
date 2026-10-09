namespace Depot.CleanArchitecture.Application.Features.Orders.CreateOrder;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class CreateOrderCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingOrder = await dbContext.DeliveryOrders
            .FirstOrDefaultAsync(o => o.OrderNumber == command.OrderNumber.Trim().ToUpperInvariant(), cancellationToken);

        if (existingOrder is not null)
        {
            return Result.Failure<CreateOrderResponse>(
                Error.Conflict("DeliveryOrder.AlreadyExists", $"Lệnh DO '{command.OrderNumber}' đã tồn tại trong hệ thống!"));
        }

        var order = DeliveryOrder.Create(
            command.TenantId,
            command.OrderNumber,
            command.LineOperator,
            command.CustomerName,
            command.CustomerTaxCode,
            command.ExpirationDate,
            command.VesselName,
            command.VoyageNo);

        foreach (var item in command.Items)
        {
            order.AddItem(item.Size, item.Type, item.RequiredGrade, item.OrderedQuantity);
        }

        dbContext.DeliveryOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateOrderResponse(
            order.Id,
            order.OrderNumber,
            order.LineOperator,
            order.ExpirationDate);

        return Result.Success(response);
    }
}
