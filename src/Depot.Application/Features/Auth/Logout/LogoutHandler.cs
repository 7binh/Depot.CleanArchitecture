using Depot.Application.Abstractions.Identity;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Application.Features.Auth.Logout;

public sealed class LogoutHandler(IRefreshTokenService refreshTokenService)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var check = BusinessRuleValidator.Check(new NotEmptyRule(command.RefreshToken, "RefreshToken"));
        if (check.IsFailure)
        {
            return check;
        }

        await refreshTokenService.RevokeAsync(command.RefreshToken!, cancellationToken);
        return Result.Success();
    }
}
