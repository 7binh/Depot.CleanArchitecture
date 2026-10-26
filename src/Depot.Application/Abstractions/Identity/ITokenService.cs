namespace Depot.Application.Abstractions.Identity;

public interface ITokenService
{
    AccessToken CreateAccessToken(AuthenticatedUser user);
}
