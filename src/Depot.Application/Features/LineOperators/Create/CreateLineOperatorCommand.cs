namespace Depot.Application.Features.LineOperators.Create;

/// <summary>Raw input for creating a line operator; the Domain trims, uppercases and validates it.</summary>
public sealed record CreateLineOperatorCommand(string? Code, string? Name);
