using System.Security.Cryptography;
using System.Text;
using Depot.Application.Abstractions.Identity;
using Depot.Domain.Common;
using Depot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Depot.Infrastructure.Identity;

public sealed class RefreshTokenService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private const int _tokenBytes = 64;

    public async Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var value = Add(userId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return value;
    }

    public async Task<Result<RefreshedSession>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);
        if (stored is null)
        {
            return InvalidToken();
        }

        var now = timeProvider.GetUtcNow();
        if (stored.RevokedAt is not null)
        {
            // A used token came back: someone may have copied it, so end every session of that user.
            await RevokeAllAsync(stored.UserId, now, cancellationToken);
            return InvalidToken();
        }

        var user = stored.ExpiresAt > now ? await userManager.FindByIdAsync(stored.UserId.ToString()) : null;
        if (user is null)
        {
            return InvalidToken();
        }

        stored.RevokedAt = now;
        var replacement = Add(user.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshedSession(new AuthenticatedUser(user.Id, user.Email!), replacement));
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);
        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        stored.RevokedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string Add(Guid userId)
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(_tokenBytes));
        var now = timeProvider.GetUtcNow();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(value),
            CreatedAt = now,
            ExpiresAt = now.AddDays(options.Value.RefreshTokenExpirationInDays)
        });

        return value;
    }

    private Task<RefreshToken?> FindAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        return dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static Result<RefreshedSession> InvalidToken()
    {
        return Result.Failure<RefreshedSession>(
            Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is not valid."));
    }
}
