namespace Depot.CleanArchitecture.Application.Features.Orders.ExtendOrder;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class ExtendOrderCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<ExtendOrderCommand>
{
    public async Task<Result> HandleAsync(
        ExtendOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure(Error.NotFound("DeliveryOrder.NotFound", $"Không tìm thấy lệnh DO với ID '{command.OrderId}'!"));
        }

        if (command.NewExpirationDate <= order.ExpirationDate)
        {
            return Result.Failure(Error.Validation("DeliveryOrder.InvalidExtensionDate", "Hạn lệnh mới phải lớn hơn hạn lệnh hiện tại!"));
        }

        order.ExtendExpirationDate(command.NewExpirationDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
