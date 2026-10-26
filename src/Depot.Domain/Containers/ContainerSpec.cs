namespace Depot.Domain.Containers;

/// <summary>Technical data of a container (BR-CT-01), grouped so that <see cref="Container.Create"/> stays short.</summary>
public sealed record ContainerSpec(
    string? IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateOnly ManufactureDate);
