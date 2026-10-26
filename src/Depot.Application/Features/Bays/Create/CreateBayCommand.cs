namespace Depot.Application.Features.Bays.Create;

/// <summary>Input for adding a bay to a block; BlockId comes from the route, BayNo from the body.</summary>
public sealed record CreateBayCommand(Guid BlockId, int BayNo);
