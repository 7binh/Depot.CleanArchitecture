namespace Depot.CleanArchitecture.Application.UnitTests.Features.Gate;

using Depot.CleanArchitecture.Application.Features.Gate.GateOut;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public sealed class GateOutCommandHandlerTests
{
    private static async Task<(InMemoryAppDbContext dbContext, Container container, ContainerVisit visit, YardSlot slot, DeliveryOrder order)>
        SetupTestDataAsync(
            string lineOperator = "MAEU",
            DateOnly? orderExpirationDate = null,
            ContainerGrade containerGrade = ContainerGrade.GradeA,
            int containerSize = 20,
            int orderSize = 20)
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        // 1. Container
        var contNum = ContainerNumber.Create("CSQU3054383").Value!;
        var container = Container.Create(
            tenantId,
            contNum,
            lineOperator,
            ContainerType.Dry,
            containerSize == 20 ? "22G1" : "42G1",
            containerSize,
            2200m,
            30480m,
            containerGrade).Value!;
        dbContext.Containers.Add(container);

        // 2. Block & Slot
        var block = YardBlock.CreatePhysical(tenantId, "A", "Block A", 20, 6, 5, false);
        dbContext.YardBlocks.Add(block);

        var slot = YardSlot.Create(tenantId, block.Id, 1, 1, 1);
        slot.Occupy(container.Id, containerSize);
        dbContext.YardSlots.Add(slot);

        // 3. Visit
        var inVehicle = VehicleInfo.Create("51C-12345", "51R-67890", "Nguyen Van In", "0901111111").Value!;
        var visit = ContainerVisit.CreateGateIn(tenantId, container.Id, lineOperator, inVehicle, containerGrade, null);
        visit.AssignSlot(slot.Id, block.Id);
        dbContext.ContainerVisits.Add(visit);

        // 4. DeliveryOrder
        var expDate = orderExpirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var order = DeliveryOrder.Create(
            tenantId,
            "DO123456",
            lineOperator,
            "Cong Ty TNHH XNK",
            "0312345678",
            expDate,
            "EVER GIVEN",
            "V001");
        order.AddItem(orderSize, ContainerType.Dry, ContainerGrade.GradeA, 2);
        dbContext.DeliveryOrders.Add(order);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (dbContext, container, visit, slot, order);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Success_When_Valid_GateOut()
    {
        // Arrange
        var (dbContext, container, visit, slot, order) = await SetupTestDataAsync();
        await using (dbContext)
        {
            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: order.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.OrderNumber.Should().Be("DO123456");

            visit.Status.Should().Be(ContainerVisitStatus.GatedOut);
            slot.IsOccupied.Should().BeFalse();
            order.Items.First().DeliveredQuantity.Should().Be(1);
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_DeliveryOrder_Expired()
    {
        // Arrange (hết hạn ngày hôm qua)
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var (dbContext, container, _, _, order) = await SetupTestDataAsync(orderExpirationDate: yesterday);
        await using (dbContext)
        {
            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: order.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeliveryOrder.Expired");
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_LineOperator_Mismatch()
    {
        // Arrange: DO hãng MAEU nhưng ta đổi hãng DO thành CMA
        var (dbContext, container, _, _, _) = await SetupTestDataAsync(lineOperator: "MAEU");
        await using (dbContext)
        {
            // Tạo DO của hãng khác
            var orderCma = DeliveryOrder.Create(
                container.TenantId,
                "DO-CMA-999",
                "CMA",
                "Khach Hang CMA",
                "0319999999",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                "VESSEL",
                "V01");
            orderCma.AddItem(20, ContainerType.Dry, ContainerGrade.GradeA, 1);
            dbContext.DeliveryOrders.Add(orderCma);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: orderCma.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeliveryOrder.LineOperatorMismatch");
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Container_Damaged()
    {
        // Arrange: container Grade D hư hỏng
        var (dbContext, container, _, _, order) = await SetupTestDataAsync(containerGrade: ContainerGrade.GradeD_Damaged);
        await using (dbContext)
        {
            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: order.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Container.DamagedCannotBeAllocated");
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Specification_Mismatch()
    {
        // Arrange: DO yêu cầu cont 40ft, nhưng cont xuất bãi là 20ft
        var (dbContext, container, _, _, order) = await SetupTestDataAsync(containerSize: 20, orderSize: 40);
        await using (dbContext)
        {
            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: order.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeliveryOrder.SpecificationMismatch");
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Quota_Exceeded()
    {
        // Arrange: Lệnh đã giao đủ quota
        var (dbContext, container, _, _, order) = await SetupTestDataAsync();
        await using (dbContext)
        {
            var item = order.Items.First();
            item.RecordDelivery();
            item.RecordDelivery(); // Đã giao 2/2 -> Quota hết
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var handler = new GateOutCommandHandler(dbContext);
            var command = new GateOutCommand(
                TenantId: container.TenantId,
                ContainerNumber: container.Number.Value,
                OrderNumber: order.OrderNumber,
                TractorNo: "51C-99999",
                TrailerNo: "51R-99999",
                DriverName: "Nguyen Van Out",
                DriverPhone: "0909999999");

            // Act
            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("DeliveryOrder.QuotaExceeded");
        }
    }
}
