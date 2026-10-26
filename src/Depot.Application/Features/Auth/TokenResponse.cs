namespace Depot.Application.Features.Auth;

/// <summary>The short-lived access token plus the long-lived refresh token used to get a new one.</summary>
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
