namespace Depot.CleanArchitecture.Application.Features.Gate.GateIn;

using FluentValidation;

public sealed class GateInValidator : AbstractValidator<GateInCommand>
{
    public GateInValidator()
    {
        RuleFor(x => x.ContainerNumber)
            .NotEmpty()
            .Length(11);

        RuleFor(x => x.LineOperator)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Size)
            .Must(s => s is 20 or 40 or 45)
            .WithMessage("Kích thước container phải là 20, 40 hoặc 45 feet.");

        RuleFor(x => x.TractorNo)
            .NotEmpty();

        RuleFor(x => x.DriverName)
            .NotEmpty();
    }
}
