namespace Depot.Application.Features.YardRows.Delete;

/// <summary>Input for removing a row; BlockId, BayId and RowId come from the route.</summary>
public sealed record DeleteYardRowCommand(Guid BlockId, Guid BayId, Guid RowId);
