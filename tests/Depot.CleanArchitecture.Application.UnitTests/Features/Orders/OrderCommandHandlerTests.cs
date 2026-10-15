namespace Depot.CleanArchitecture.Application.UnitTests.Features.Orders;

using Depot.CleanArchitecture.Application.Features.Orders.CreateOrder;
using Depot.CleanArchitecture.Application.Features.Orders.ExtendOrder;
using Depot.CleanArchitecture.Domain.Enums;
using FluentAssertions;
using Xunit;

public sealed class OrderCommandHandlerTests
{
    [Fact]
    public async Task CreateOrder_Should_Return_Success_When_Valid()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateOrderCommandHandler(dbContext);
        var tenantId = Guid.NewGuid();
        var expDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var items = new List<CreateOrderItemDto>
        {
            new(20, ContainerType.Dry, ContainerGrade.GradeA, 5)
        };

        var command = new CreateOrderCommand(
            tenantId,
            "DO-SNP-001",
            "MAEU",
            "Cong Ty TNHH Logistics",
            "0301234567",
            expDate,
            "MAERSK MC-KINNEY MOLLER",
            "VOY101",
            items);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("DO-SNP-001");
        dbContext.DeliveryOrders.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExtendOrder_Should_Update_ExpirationDate_When_NewDate_Is_Later()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var initialExpDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        var createHandler = new CreateOrderCommandHandler(dbContext);
        var createCommand = new CreateOrderCommand(
            tenantId,
            "DO-SNP-002",
            "ONE",
            "Cong Ty ABC",
            "0309999999",
            initialExpDate,
            "ONE APUS",
            "V002",
            [new(40, ContainerType.Dry, ContainerGrade.GradeA, 2)]);

        var createResult = await createHandler.HandleAsync(createCommand, TestContext.Current.CancellationToken);
        createResult.IsSuccess.Should().BeTrue();

        var extendHandler = new ExtendOrderCommandHandler(dbContext);
        var newExpDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var extendCommand = new ExtendOrderCommand(createResult.Value!.OrderId, newExpDate);

        // Act
        var extendResult = await extendHandler.HandleAsync(extendCommand, TestContext.Current.CancellationToken);

        // Assert
        extendResult.IsSuccess.Should().BeTrue();
        var updatedOrder = dbContext.DeliveryOrders.Find(createResult.Value.OrderId);
        updatedOrder!.ExpirationDate.Should().Be(newExpDate);
    }
}
