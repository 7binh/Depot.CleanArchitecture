namespace Depot.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSecretLength = 32;

    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpirationInMinutes { get; init; }

    public int RefreshTokenExpirationInDays { get; init; }
}
