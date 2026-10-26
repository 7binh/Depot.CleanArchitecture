using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Auth;
using Depot.Application.Features.Auth.Login;
using Depot.Application.Features.Auth.Logout;
using Depot.Application.Features.Auth.Refresh;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Sign in with email and password and receive an access token and a refresh token");

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Swap a refresh token for a new access token and a new refresh token");

        group.MapPost("/logout", Logout)
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Revoke a refresh token so it can no longer be used");
    }

    private static async Task<IResult> Login(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommand command,
        ICommandHandler<RefreshTokenCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Logout(
        LogoutCommand command,
        ICommandHandler<LogoutCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
