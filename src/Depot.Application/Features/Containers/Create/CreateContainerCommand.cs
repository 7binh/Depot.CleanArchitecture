namespace Depot.Application.Features.Containers.Create;

/// <summary>
/// Raw input for registering a container. There is no line operator id: the operator is found from the
/// owner code (first 3 letters) of the number (BR-CT-09, A-15).
/// </summary>
public sealed record CreateContainerCommand(
    string? Number,
    Guid ContainerTypeId,
    string? IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateOnly ManufactureDate);
