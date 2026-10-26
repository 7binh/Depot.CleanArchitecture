using Depot.Application.Features.Auth.Logout;
using Depot.Domain.Common;

namespace Depot.Application.Tests.Features.Auth;

public sealed class LogoutHandlerTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_RevokesTheRefreshToken()
    {
        var refresh = new FakeRefreshTokenService();
        var handler = new LogoutHandler(refresh);

        var result = await handler.HandleAsync(new LogoutCommand("my-token"), _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("my-token", refresh.RevokedToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_ReturnsValidationError_AndRevokesNothing_WhenTokenIsEmpty(string? token)
    {
        var refresh = new FakeRefreshTokenService();
        var handler = new LogoutHandler(refresh);

        var result = await handler.HandleAsync(new LogoutCommand(token), _ct);

        Assert.True(result.IsFailure);
        Assert.Equal("RefreshToken.Empty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Null(refresh.RevokedToken);
    }
}
