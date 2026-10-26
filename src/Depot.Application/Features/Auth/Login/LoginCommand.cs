namespace Depot.Application.Features.Auth.Login;

public sealed record LoginCommand(string? Email, string? Password);
