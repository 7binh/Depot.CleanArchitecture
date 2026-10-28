namespace Depot.Application.Features.ContainerStays.Move;

/// <summary>
/// Raw input for moving a container that is in the yard to another position. <c>RowId</c> and <c>Tier</c> stay
/// empty for a virtual block, as in gate in.
/// </summary>
public sealed record MoveContainerCommand(Guid StayId, Guid BlockId, Guid? RowId, int? Tier);
