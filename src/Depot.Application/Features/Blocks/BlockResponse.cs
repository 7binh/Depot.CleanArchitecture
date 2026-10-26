namespace Depot.Application.Features.Blocks;

public sealed record BlockResponse(Guid Id, string Code, string Name, bool IsVirtual);
