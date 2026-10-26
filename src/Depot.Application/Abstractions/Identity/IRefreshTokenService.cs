using Depot.Domain.Common;

namespace Depot.Application.Abstractions.Identity;

/// <summary>Issues, rotates and revokes refresh tokens. Implemented in Infrastructure, which stores only a hash of each token.</summary>
public interface IRefreshTokenService
{
    /// <summary>Creates a refresh token for the user and returns its value (the only time it is visible).</summary>
    Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Swaps a valid refresh token for a new one (the old one stops working).
    /// Fails with Unauthorized when the token is unknown, expired or already used.
    /// </summary>
    Task<Result<RefreshedSession>> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Makes the refresh token unusable. Does nothing when it is unknown.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
