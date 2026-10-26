namespace Depot.Application.Abstractions.Identity;

/// <summary>The user a refresh token belongs to, plus the replacement refresh token that was issued.</summary>
public sealed record RefreshedSession(AuthenticatedUser User, string RefreshToken);
