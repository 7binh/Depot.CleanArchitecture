namespace Depot.Application.Abstractions.Identity;

public sealed record AuthenticatedUser(Guid Id, string Email);
