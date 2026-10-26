using Microsoft.AspNetCore.Identity;

namespace Depot.Infrastructure.Identity;

/// <summary>Login account. Lives in Infrastructure on purpose: Domain must not depend on ASP.NET Identity.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
