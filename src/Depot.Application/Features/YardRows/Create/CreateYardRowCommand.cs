namespace Depot.Application.Features.YardRows.Create;

/// <summary>Input for adding a row to a bay; BlockId and BayId come from the route, the rest from the body.</summary>
public sealed record CreateYardRowCommand(Guid BlockId, Guid BayId, int RowNo, int MaxTier);
