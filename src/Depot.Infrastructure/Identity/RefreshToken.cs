namespace Depot.Infrastructure.Identity;

/// <summary>A stored refresh token. Only the SHA-256 hash is kept, so a leaked database cannot be used to sign in.</summary>
public sealed class RefreshToken
{
    public const int HashLength = 64;

    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public string TokenHash { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }
}
