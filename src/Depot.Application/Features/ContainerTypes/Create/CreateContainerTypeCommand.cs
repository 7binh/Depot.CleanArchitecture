namespace Depot.Application.Features.ContainerTypes.Create;

/// <summary>Raw input for creating a container type; the Domain trims, uppercases and validates it.</summary>
public sealed record CreateContainerTypeCommand(string? Code, string? Name);
