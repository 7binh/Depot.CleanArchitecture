namespace Depot.Application.Features.ContainerStays.GetAll;

/// <param name="InYard">True lists only the stays still open, i.e. the containers now in the yard.</param>
public sealed record GetContainerStaysQuery(bool InYard = false);
