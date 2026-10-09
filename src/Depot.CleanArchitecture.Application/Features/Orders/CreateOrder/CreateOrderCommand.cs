namespace Depot.CleanArchitecture.Application.Features.Orders.CreateOrder;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public sealed record CreateOrderItemDto(
    int Size,
    ContainerType Type,
    ContainerGrade RequiredGrade,
    int OrderedQuantity);

public sealed record CreateOrderCommand(
    Guid TenantId,
    string OrderNumber,
    string LineOperator,
    string CustomerName,
    string CustomerTaxCode,
    DateOnly ExpirationDate,
    string VesselName,
    string VoyageNo,
    List<CreateOrderItemDto> Items) : ICommand<Result<CreateOrderResponse>>;

public sealed record CreateOrderResponse(
    Guid OrderId,
    string OrderNumber,
    string LineOperator,
    DateOnly ExpirationDate);
