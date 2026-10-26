namespace Depot.Application.Features.Containers;

public sealed record ContainerResponse(
    Guid Id,
    string Number,
    Guid LineOperatorId,
    Guid ContainerTypeId,
    string IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateOnly ManufactureDate);
