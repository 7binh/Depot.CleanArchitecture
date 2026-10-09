namespace Depot.CleanArchitecture.Application.Features.Orders.CreateOrder;

using FluentValidation;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LineOperator).NotEmpty().MaximumLength(10);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerTaxCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Lệnh giao container phải có ít nhất 1 dòng hàng.");
    }
}
