namespace Depot.Application.Features.Bays;

public sealed record BayResponse(Guid Id, Guid BlockId, int BayNo);
