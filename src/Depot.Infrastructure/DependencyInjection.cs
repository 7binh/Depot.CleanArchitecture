using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Identity;
using Depot.Infrastructure.Identity;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Depot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => x.Secret.Length >= JwtOptions.MinSecretLength, "Jwt:Secret must be at least 32 characters.")
            .Validate(x => x.ExpirationInMinutes > 0, "Jwt:ExpirationInMinutes must be positive.")
            .Validate(x => x.RefreshTokenExpirationInDays > 0, "Jwt:RefreshTokenExpirationInDays must be positive.")
            .ValidateOnStart();

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
