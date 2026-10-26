namespace Depot.Application.Features.YardRows;

public sealed record YardRowResponse(Guid Id, Guid BayId, int RowNo, int MaxTier);
