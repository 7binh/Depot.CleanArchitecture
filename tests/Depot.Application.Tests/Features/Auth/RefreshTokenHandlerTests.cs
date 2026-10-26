using Depot.Application.Abstractions.Identity;
using Depot.Application.Features.Auth.Refresh;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Auth;

public sealed class RefreshTokenHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly AuthenticatedUser _user = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task HandleAsync_ReturnsNewAccessTokenAndTheRotatedRefreshToken()
    {
        var handler = new RefreshTokenHandler(new FakeTokenService(), new FakeRefreshTokenService(_user));

        var result = await handler.HandleAsync(new RefreshTokenCommand("old-token"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal($"token-for-{_user.Id}", result.Value.AccessToken);
        Assert.Equal(FakeRefreshTokenService.RotatedToken, result.Value.RefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task HandleAsync_ReturnsValidationError_WhenTokenIsEmpty(string? token)
    {
        var handler = new RefreshTokenHandler(new FakeTokenService(), new FakeRefreshTokenService(_user));

        var result = await handler.HandleAsync(new RefreshTokenCommand(token), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("RefreshToken.Empty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenTheRefreshTokenIsNotValid()
    {
        var handler = new RefreshTokenHandler(new FakeTokenService(), new FakeRefreshTokenService());

        var result = await handler.HandleAsync(new RefreshTokenCommand("bad-token"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }
}
