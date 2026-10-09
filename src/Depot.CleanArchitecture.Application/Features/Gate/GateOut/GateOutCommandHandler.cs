namespace Depot.CleanArchitecture.Application.Features.Gate.GateOut;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.Rules.Orders;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class GateOutCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<GateOutCommand, Result<GateOutResponse>>
{
    public async Task<Result<GateOutResponse>> HandleAsync(
        GateOutCommand command,
        CancellationToken cancellationToken = default)
    {
        var containerNumberResult = ContainerNumber.Create(command.ContainerNumber);
        if (containerNumberResult.IsFailure)
        {
            return Result.Failure<GateOutResponse>(containerNumberResult.Error);
        }

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.Number.Value == containerNumberResult.Value!.Value, cancellationToken);

        if (container is null)
        {
            return Result.Failure<GateOutResponse>(
                Error.NotFound("Container.NotFound", $"Không tìm thấy container '{command.ContainerNumber}'!"));
        }

        var activeVisit = await dbContext.ContainerVisits
            .FirstOrDefaultAsync(v => v.ContainerId == container.Id &&
                                      v.Status != ContainerVisitStatus.GatedOut &&
                                      v.Status != ContainerVisitStatus.Completed, cancellationToken);

        if (activeVisit is null)
        {
            return Result.Failure<GateOutResponse>(
                Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt vào bãi (visit) đang hoạt động của container '{command.ContainerNumber}'!"));
        }

        var order = await dbContext.DeliveryOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == command.OrderNumber.Trim().ToUpperInvariant(), cancellationToken);

        if (order is null)
        {
            return Result.Failure<GateOutResponse>(
                Error.NotFound("DeliveryOrder.NotFound", $"Không tìm thấy Lệnh giao container (DO) '{command.OrderNumber}'!"));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Kiểm tra Hạn lệnh (Rule 1)
        var notExpiredRule = new DeliveryOrderNotExpiredRule(order.ExpirationDate, today);
        if (notExpiredRule.IsBroken())
        {
            return Result.Failure<GateOutResponse>(Error.Validation(notExpiredRule.RuleCode, notExpiredRule.Message));
        }

        // 2. Kiểm tra Hãng tàu khớp nhau (Rule 2)
        var lineOperatorMatchRule = new ContainerLineOperatorMatchesOrderRule(activeVisit.LineOperator, order.LineOperator);
        if (lineOperatorMatchRule.IsBroken())
        {
            return Result.Failure<GateOutResponse>(Error.Validation(lineOperatorMatchRule.RuleCode, lineOperatorMatchRule.Message));
        }

        // 3. Kiểm tra Container hư hỏng Grade D / E (Rule 3)
        var notDamagedRule = new DamagedContainerCannotBeAllocatedRule(container.Grade);
        if (notDamagedRule.IsBroken())
        {
            return Result.Failure<GateOutResponse>(Error.Validation(notDamagedRule.RuleCode, notDamagedRule.Message));
        }

        // 4. Kiểm tra Quy cách (Rule 4) & Định ngạch (Rule 5)
        var matchingItem = order.Items.FirstOrDefault(i =>
            i.Size == container.Size &&
            i.Type == container.Type &&
            container.Grade <= i.RequiredGrade);

        if (matchingItem is null)
        {
            var firstItem = order.Items.FirstOrDefault();
            var specRule = new ContainerMatchesOrderSpecificationRule(
                container.Size,
                container.Type,
                container.Grade,
                firstItem?.Size ?? 0,
                firstItem?.Type ?? container.Type,
                firstItem?.RequiredGrade ?? container.Grade);

            return Result.Failure<GateOutResponse>(Error.Validation(specRule.RuleCode, specRule.Message));
        }

        var quotaRule = new DeliveryOrderQuotaRemainingRule(matchingItem.DeliveredQuantity, matchingItem.OrderedQuantity);
        if (quotaRule.IsBroken())
        {
            return Result.Failure<GateOutResponse>(Error.Validation(quotaRule.RuleCode, quotaRule.Message));
        }

        // 5. Thỏa mãn toàn bộ 5 Business Rules -> Thực hiện xuất bãi
        var outVehicleResult = VehicleInfo.Create(
            command.TractorNo,
            command.TrailerNo,
            command.DriverName,
            command.DriverPhone);

        if (outVehicleResult.IsFailure)
        {
            return Result.Failure<GateOutResponse>(outVehicleResult.Error);
        }

        // Cập nhật số lượng đã giao trên DO item
        matchingItem.RecordDelivery();

        // Giải phóng ô bãi nếu đang ở slot
        if (activeVisit.CurrentSlotId.HasValue)
        {
            var slot = await dbContext.YardSlots.FindAsync([activeVisit.CurrentSlotId.Value], cancellationToken);
            slot?.Release();
        }

        // Chuyển trạng thái visit sang GatedOut
        activeVisit.AllocateToOrder(order.Id);
        activeVisit.GateOut(outVehicleResult.Value!);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GateOutResponse(
            activeVisit.Id,
            container.Number.Value,
            order.OrderNumber,
            activeVisit.GateOutDate!.Value);

        return Result.Success(response);
    }
}
