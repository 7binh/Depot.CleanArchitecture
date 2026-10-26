namespace Depot.Application.Features.LineOperators;

/// <summary>What the caller gets back when a line operator is created or read.</summary>
public sealed record LineOperatorResponse(Guid Id, string Code, string Name);
