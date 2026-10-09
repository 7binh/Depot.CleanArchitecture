namespace Depot.CleanArchitecture.Application.Features.Orders.ExtendOrder;

using FluentValidation;

public sealed class ExtendOrderValidator : AbstractValidator<ExtendOrderCommand>
{
    public ExtendOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
