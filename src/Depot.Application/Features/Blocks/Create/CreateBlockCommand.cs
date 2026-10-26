namespace Depot.Application.Features.Blocks.Create;

/// <summary>Raw input for creating a block; the Domain trims, uppercases and validates it.</summary>
public sealed record CreateBlockCommand(string? Code, string? Name, bool IsVirtual = false);
