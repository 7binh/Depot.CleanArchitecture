namespace Depot.CleanArchitecture.Application.Features.Yard.PlaceContainer;

using FluentValidation;

public sealed class PlaceContainerValidator : AbstractValidator<PlaceContainerCommand>
{
    public PlaceContainerValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.Bay).GreaterThan(0);
        RuleFor(x => x.Row).GreaterThan(0);
        RuleFor(x => x.Tier).GreaterThan(0);
    }
}
