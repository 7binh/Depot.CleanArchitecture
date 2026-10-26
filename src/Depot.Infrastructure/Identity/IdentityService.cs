using Depot.Application.Abstractions.Identity;
using Depot.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Depot.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher) : IIdentityService
{
    // Hashed once so that an unknown email costs about the same time as a wrong password.
    private static readonly Lazy<string> _decoyHash = new(
        () => new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString()));

    public async Task<Result<AuthenticatedUser>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(new ApplicationUser(), _decoyHash.Value, password);
            return InvalidCredentials();
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return InvalidCredentials();
        }

        return Result.Success(new AuthenticatedUser(user.Id, user.Email!));
    }

    private static Result<AuthenticatedUser> InvalidCredentials()
    {
        return Result.Failure<AuthenticatedUser>(
            Error.Unauthorized("Auth.InvalidCredentials", "Wrong email or password."));
    }
}
