using Depot.Application.Abstractions.Identity;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Auth;

internal sealed class FakeIdentityService(AuthenticatedUser? user) : IIdentityService
{
    public bool WasCalled { get; private set; }

    public string? ReceivedEmail { get; private set; }

    public Task<Result<AuthenticatedUser>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        ReceivedEmail = email;
        Result<AuthenticatedUser> result = user is null
            ? Result.Failure<AuthenticatedUser>(Error.Unauthorized("Auth.InvalidCredentials", "Wrong email or password."))
            : Result.Success(user);
        return Task.FromResult(result);
    }
}

internal sealed class FakeTokenService : ITokenService
{
    public static readonly DateTimeOffset ExpiresAt = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    public AccessToken CreateAccessToken(AuthenticatedUser user)
    {
        return new AccessToken($"token-for-{user.Id}", ExpiresAt);
    }
}

internal sealed class FakeRefreshTokenService(AuthenticatedUser? sessionUser = null) : IRefreshTokenService
{
    public const string IssuedToken = "issued-refresh-token";
    public const string RotatedToken = "rotated-refresh-token";

    public Guid? IssuedForUserId { get; private set; }

    public string? RevokedToken { get; private set; }

    public Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IssuedForUserId = userId;
        return Task.FromResult(IssuedToken);
    }

    public Task<Result<RefreshedSession>> RotateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        Result<RefreshedSession> result = sessionUser is null
            ? Result.Failure<RefreshedSession>(Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is not valid."))
            : Result.Success(new RefreshedSession(sessionUser, RotatedToken));
        return Task.FromResult(result);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RevokedToken = refreshToken;
        return Task.CompletedTask;
    }
}
