namespace Depot.Domain.Gate;

/// <summary>
/// Tiers in the yard that hold a container, in the rows with the same row number in the bays next to the target
/// bay. A 40ft container in an even bay N covers the odd bays N-1 and N+1, so the bays around a position
/// are part of the same floor space. Application loads it; the Domain never queries.
/// </summary>
/// <param name="Before">Bay N-1.</param>
/// <param name="After">Bay N+1.</param>
/// <param name="FarBefore">Bay N-2.</param>
/// <param name="FarAfter">Bay N+2.</param>
public sealed record NeighbourTiers(
    IReadOnlyCollection<int> Before,
    IReadOnlyCollection<int> After,
    IReadOnlyCollection<int> FarBefore,
    IReadOnlyCollection<int> FarAfter)
{
    /// <summary>No container around: a virtual block, or a bay with nothing beside it.</summary>
    public static NeighbourTiers None { get; } = new([], [], [], []);
}
