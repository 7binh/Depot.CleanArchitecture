namespace Depot.Application.Features.YardRows.GetById;

public sealed record GetYardRowByIdQuery(Guid BlockId, Guid BayId, Guid RowId);
