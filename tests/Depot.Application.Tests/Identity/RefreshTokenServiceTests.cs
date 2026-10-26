using Depot.Domain.Common;
using Depot.Infrastructure.Identity;
using Depot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Depot.Application.Tests.Identity;

public sealed class RefreshTokenServiceTests
{
    private const string Email = "user@example.com";

    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _start = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IssueAsync_StoresOnlyAHashOfTheToken()
    {
        await using var fixture = await Fixture.CreateAsync();

        var token = await fixture.Service.IssueAsync(fixture.UserId, _ct);

        var stored = await fixture.DbContext.RefreshTokens.SingleAsync(_ct);
        Assert.NotEqual(token, stored.TokenHash);
        Assert.Equal(_start.AddDays(7), stored.ExpiresAt);
        Assert.Null(stored.RevokedAt);
    }

    [Fact]
    public async Task RotateAsync_ReturnsTheUserAndANewToken_AndRetiresTheOldOne()
    {
        await using var fixture = await Fixture.CreateAsync();
        var first = await fixture.Service.IssueAsync(fixture.UserId, _ct);

        var rotated = await fixture.Service.RotateAsync(first, _ct);

        Assert.True(rotated.IsSuccess);
        Assert.Equal(Email, rotated.Value.User.Email);
        Assert.NotEqual(first, rotated.Value.RefreshToken);
        Assert.Equal(1, await fixture.DbContext.RefreshTokens.CountAsync(x => x.RevokedAt == null, _ct));
    }

    [Fact]
    public async Task RotateAsync_RevokesEverySession_WhenAnAlreadyUsedTokenComesBack()
    {
        await using var fixture = await Fixture.CreateAsync();
        var first = await fixture.Service.IssueAsync(fixture.UserId, _ct);
        var second = (await fixture.Service.RotateAsync(first, _ct)).Value.RefreshToken;

        var replay = await fixture.Service.RotateAsync(first, _ct);
        var afterReplay = await fixture.Service.RotateAsync(second, _ct);

        Assert.True(replay.IsFailure);
        Assert.True(afterReplay.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, afterReplay.Error.Type);
    }

    [Fact]
    public async Task RotateAsync_Fails_WhenTheTokenIsUnknown()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.RotateAsync("not-a-real-token", _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task RotateAsync_Fails_WhenTheTokenHasExpired()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueAsync(fixture.UserId, _ct);
        fixture.Clock.Advance(TimeSpan.FromDays(8));

        var result = await fixture.Service.RotateAsync(token, _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task RevokeAsync_MakesTheTokenUnusable_AndIgnoresUnknownTokens()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueAsync(fixture.UserId, _ct);

        await fixture.Service.RevokeAsync(token, _ct);
        await fixture.Service.RevokeAsync("not-a-real-token", _ct);
        var result = await fixture.Service.RotateAsync(token, _ct);

        Assert.True(result.IsFailure);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        private Fixture(ServiceProvider provider, IServiceScope scope, Guid userId, MutableTimeProvider clock)
        {
            _provider = provider;
            _scope = scope;
            UserId = userId;
            Clock = clock;
            DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var options = Options.Create(new JwtOptions { RefreshTokenExpirationInDays = 7 });
            Service = new RefreshTokenService(
                DbContext,
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                options,
                clock);
        }

        public Guid UserId { get; }

        public MutableTimeProvider Clock { get; }

        public AppDbContext DbContext { get; }

        public RefreshTokenService Service { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var provider = IdentityTestHost.Build();
            var scope = provider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = Email, Email = Email };
            var created = await userManager.CreateAsync(user, "Passw0rd!");
            Assert.True(created.Succeeded);
            return new Fixture(provider, scope, user.Id, new MutableTimeProvider(_start));
        }

        public ValueTask DisposeAsync()
        {
            _scope.Dispose();
            return _provider.DisposeAsync();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public void Advance(TimeSpan by)
        {
            _now += by;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
