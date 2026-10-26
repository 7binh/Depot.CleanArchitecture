namespace Depot.Application.Features.Bays.Delete;

/// <summary>Input for removing a bay together with its rows; BlockId and BayId come from the route.</summary>
public sealed record DeleteBayCommand(Guid BlockId, Guid BayId);
