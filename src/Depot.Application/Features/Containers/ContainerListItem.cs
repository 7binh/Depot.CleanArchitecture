namespace Depot.Application.Features.Containers;

/// <summary>A container with its operator and type codes already worked out, so a list needs no second call.</summary>
public sealed record ContainerListItem(
    Guid Id,
    string Number,
    Guid LineOperatorId,
    string LineOperatorCode,
    Guid ContainerTypeId,
    string ContainerTypeCode,
    string IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateOnly ManufactureDate);
