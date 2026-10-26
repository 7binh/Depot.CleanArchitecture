using Depot.Domain.Yard;

namespace Depot.Domain.Gate;

/// <summary>
/// Where the container is put, with the yard state the rules need. Application loads it; the Domain never queries.
/// <paramref name="Bay"/>, <paramref name="Row"/> and <paramref name="Tier"/> are empty for a virtual block (BR-YD-10).
/// <paramref name="OccupiedTiers"/> are the tiers of <paramref name="Row"/> that already hold a container in the yard.
/// <paramref name="Neighbours"/> are the same for the bays beside it; empty means nothing is there.
/// </summary>
public sealed record GateInPlacement(
    Block Block,
    Bay? Bay,
    YardRow? Row,
    int? Tier,
    IReadOnlyCollection<int> OccupiedTiers,
    NeighbourTiers? Neighbours = null);
