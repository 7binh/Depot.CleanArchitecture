using Depot.Application.Abstractions.Identity;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService)
    : ICommandHandler<LoginCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email?.Trim();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(email, "Email"),
            new NotEmptyRule(command.Password, "Password"));

        if (check.IsFailure)
        {
            return Result.Failure<TokenResponse>(check.Error);
        }

        var user = await identityService.CheckCredentialsAsync(email!, command.Password!, cancellationToken);
        if (user.IsFailure)
        {
            return Result.Failure<TokenResponse>(user.Error);
        }

        var accessToken = tokenService.CreateAccessToken(user.Value);
        var refreshToken = await refreshTokenService.IssueAsync(user.Value.Id, cancellationToken);
        return Result.Success(new TokenResponse(accessToken.Value, accessToken.ExpiresAt, refreshToken));
    }
}
