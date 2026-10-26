using Depot.Application.Abstractions.Identity;
using Depot.Application.Features.Auth.Login;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Auth;

public sealed class LoginHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly AuthenticatedUser _user = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task HandleAsync_ReturnsBothTokens_WhenCredentialsAreCorrect()
    {
        var identity = new FakeIdentityService(_user);
        var refresh = new FakeRefreshTokenService();
        var handler = new LoginHandler(identity, new FakeTokenService(), refresh);

        var result = await handler.HandleAsync(new LoginCommand("  user@example.com ", "secret"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal($"token-for-{_user.Id}", result.Value.AccessToken);
        Assert.Equal(FakeTokenService.ExpiresAt, result.Value.ExpiresAt);
        Assert.Equal(FakeRefreshTokenService.IssuedToken, result.Value.RefreshToken);
        Assert.Equal("user@example.com", identity.ReceivedEmail);
        Assert.Equal(_user.Id, refresh.IssuedForUserId);
    }

    [Theory]
    [InlineData(null, "secret", "Email.Empty")]
    [InlineData("  ", "secret", "Email.Empty")]
    [InlineData("user@example.com", null, "Password.Empty")]
    [InlineData("user@example.com", "", "Password.Empty")]
    public async Task HandleAsync_ReturnsValidationError_AndSkipsTheIdentityCheck_WhenInputIsEmpty(
        string? email,
        string? password,
        string expectedCode)
    {
        var identity = new FakeIdentityService(_user);
        var handler = new LoginHandler(identity, new FakeTokenService(), new FakeRefreshTokenService());

        var result = await handler.HandleAsync(new LoginCommand(email, password), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.False(identity.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_AndIssuesNoRefreshToken_WhenCredentialsAreWrong()
    {
        var refresh = new FakeRefreshTokenService();
        var handler = new LoginHandler(new FakeIdentityService(null), new FakeTokenService(), refresh);

        var result = await handler.HandleAsync(new LoginCommand("user@example.com", "wrong"), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Null(refresh.IssuedForUserId);
    }
}
