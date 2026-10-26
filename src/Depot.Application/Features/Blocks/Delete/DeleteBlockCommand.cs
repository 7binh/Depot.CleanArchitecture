namespace Depot.Application.Features.Blocks.Delete;

/// <summary>Input for removing a block together with its bays and rows; Id comes from the route.</summary>
public sealed record DeleteBlockCommand(Guid Id);
