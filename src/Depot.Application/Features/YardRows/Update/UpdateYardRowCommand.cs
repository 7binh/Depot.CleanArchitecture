namespace Depot.Application.Features.YardRows.Update;

/// <summary>Input for changing a row's maximum tier; BlockId, BayId and RowId come from the route.</summary>
public sealed record UpdateYardRowCommand(Guid BlockId, Guid BayId, Guid RowId, int MaxTier);
