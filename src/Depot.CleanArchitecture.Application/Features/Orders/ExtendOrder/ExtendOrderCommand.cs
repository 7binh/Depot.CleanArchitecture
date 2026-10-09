namespace Depot.CleanArchitecture.Application.Features.Orders.ExtendOrder;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record ExtendOrderCommand(Guid OrderId, DateOnly NewExpirationDate) : ICommand;
