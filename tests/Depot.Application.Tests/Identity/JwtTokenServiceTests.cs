using System.Security.Claims;
using System.Text;
using Depot.Application.Abstractions.Identity;
using Depot.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Depot.Application.Tests.Identity;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions _options = new()
    {
        Secret = "test-secret-test-secret-test-secret-123",
        Issuer = "Depot.Tests",
        Audience = "Depot.Tests.Client",
        ExpirationInMinutes = 30
    };

    private static readonly AuthenticatedUser _user = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task CreateAccessToken_ReturnsTokenCarryingTheUserIdAndEmail()
    {
        var service = CreateService(DateTimeOffset.UtcNow);

        var token = service.CreateAccessToken(_user);

        var validated = await ValidateAsync(token.Value);
        Assert.True(validated.IsValid);
        Assert.Equal(_user.Id.ToString(), validated.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(_user.Email, validated.ClaimsIdentity.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void CreateAccessToken_ExpiresAfterTheConfiguredMinutes()
    {
        var now = new DateTimeOffset(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);
        var service = CreateService(now);

        var token = service.CreateAccessToken(_user);

        Assert.Equal(now.AddMinutes(30), token.ExpiresAt);
    }

    [Fact]
    public async Task CreateAccessToken_ProducesATokenThatIsRejectedAfterItExpires()
    {
        var service = CreateService(DateTimeOffset.UtcNow.AddHours(-2));

        var token = service.CreateAccessToken(_user);

        var validated = await ValidateAsync(token.Value);
        Assert.False(validated.IsValid);
    }

    private static JwtTokenService CreateService(DateTimeOffset now)
    {
        return new JwtTokenService(Options.Create(_options), new FixedTimeProvider(now));
    }

    private static Task<TokenValidationResult> ValidateAsync(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ClockSkew = TimeSpan.Zero
        };
        return new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
