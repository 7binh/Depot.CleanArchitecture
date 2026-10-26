using Depot.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Depot.Application.Tests.Identity;

public sealed class AdminSeederTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly Dictionary<string, string?> _settings = new()
    {
        ["Seed:AdminEmail"] = "admin@example.com",
        ["Seed:AdminPassword"] = "Admin@123"
    };

    [Fact]
    public async Task SeedAdminAsync_CreatesTheAdmin_WhenConfigured()
    {
        await using var provider = IdentityTestHost.Build(_settings);

        await provider.SeedAdminAsync();

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(admin);
        Assert.True(await userManager.CheckPasswordAsync(admin, "Admin@123"));
    }

    [Fact]
    public async Task SeedAdminAsync_RunTwice_StillLeavesOneAdmin()
    {
        await using var provider = IdentityTestHost.Build(_settings);

        await provider.SeedAdminAsync();
        await provider.SeedAdminAsync();

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(1, await userManager.Users.CountAsync(_ct));
    }

    [Fact]
    public async Task SeedAdminAsync_CreatesNothing_WhenNotConfigured()
    {
        await using var provider = IdentityTestHost.Build();

        await provider.SeedAdminAsync();

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(0, await userManager.Users.CountAsync(_ct));
    }
}
