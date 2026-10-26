using Depot.Domain.Common;

namespace Depot.Application.Abstractions.Identity;

/// <summary>Checks who a user is. Implemented in Infrastructure (ASP.NET Identity); Application never sees the password hash.</summary>
public interface IIdentityService
{
    /// <summary>Succeeds with the user, or fails with Unauthorized when the email or password is wrong.</summary>
    Task<Result<AuthenticatedUser>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
