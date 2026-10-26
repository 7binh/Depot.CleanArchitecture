namespace Depot.Application.Features.Blocks.Update;

/// <summary>Input for renaming a block; Id comes from the route, Name from the body.</summary>
public sealed record UpdateBlockCommand(Guid Id, string? Name);
