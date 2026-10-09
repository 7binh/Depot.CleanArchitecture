namespace Depot.CleanArchitecture.Application.Features.Gate.GateOut;

using FluentValidation;

public sealed class GateOutValidator : AbstractValidator<GateOutCommand>
{
    public GateOutValidator()
    {
        RuleFor(x => x.ContainerNumber)
            .NotEmpty()
            .Length(11);

        RuleFor(x => x.OrderNumber)
            .NotEmpty();

        RuleFor(x => x.TractorNo)
            .NotEmpty();

        RuleFor(x => x.DriverName)
            .NotEmpty();
    }
}
