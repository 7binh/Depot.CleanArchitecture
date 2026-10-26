using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Depot.Infrastructure.Identity;

public static class AdminSeeder
{
    /// <summary>Creates the first account from configuration (Seed:AdminEmail / Seed:AdminPassword) if it does not exist yet.</summary>
    public static async Task SeedAdminAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminSeeder));

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("No Seed:AdminEmail / Seed:AdminPassword configured; skipping admin seed.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            var reasons = string.Join("; ", created.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Could not seed the admin account: {reasons}");
        }

        logger.LogInformation("Seeded the admin account.");
    }
}
