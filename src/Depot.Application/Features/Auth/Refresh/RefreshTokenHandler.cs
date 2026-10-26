using Depot.Application.Abstractions.Identity;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Application.Features.Auth.Refresh;

public sealed class RefreshTokenHandler(ITokenService tokenService, IRefreshTokenService refreshTokenService)
    : ICommandHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var check = BusinessRuleValidator.Check(new NotEmptyRule(command.RefreshToken, "RefreshToken"));
        if (check.IsFailure)
        {
            return Result.Failure<TokenResponse>(check.Error);
        }

        var session = await refreshTokenService.RotateAsync(command.RefreshToken!, cancellationToken);
        if (session.IsFailure)
        {
            return Result.Failure<TokenResponse>(session.Error);
        }

        var accessToken = tokenService.CreateAccessToken(session.Value.User);
        return Result.Success(new TokenResponse(accessToken.Value, accessToken.ExpiresAt, session.Value.RefreshToken));
    }
}
