namespace Depot.Application.Features.Blocks.GetMap;

/// <summary>
/// Everything a screen needs to draw one block: its bays, rows and the container on each tier.
/// <paramref name="Unplaced"/> lists the containers that stand in the block without a position (a virtual block).
/// </summary>
public sealed record BlockMapResponse(
    Guid BlockId,
    string Code,
    string Name,
    bool IsVirtual,
    IReadOnlyList<BayMap> Bays,
    IReadOnlyList<MapContainer> Unplaced);

/// <summary>Only bays that exist are listed, so a removed bay is a gap in the numbering.</summary>
public sealed record BayMap(int BayNo, int SizeFeet, IReadOnlyList<RowMap> Rows);

/// <summary>Tiers lists only the tiers that hold a container, from the bottom up; an empty list is an empty stack.</summary>
public sealed record RowMap(int RowNo, int MaxTier, IReadOnlyList<TierMap> Tiers);

/// <summary>FromBay and ToBay are the bays the container covers on the floor: its own bay, or for a 40ft container also the odd bays beside it.</summary>
public sealed record TierMap(int Tier, int FromBay, int ToBay, MapContainer Container);

public sealed record MapContainer(
    Guid StayId,
    string Number,
    int SizeFeet,
    string OperatorCode,
    int DaysInYard,
    bool IsLongStay,
    bool IsDamaged);
