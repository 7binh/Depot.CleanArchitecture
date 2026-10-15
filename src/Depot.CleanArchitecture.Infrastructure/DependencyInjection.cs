namespace Depot.CleanArchitecture.Infrastructure;

using Depot.CleanArchitecture.Application.Abstractions.Caching;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Identity;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Infrastructure.Caching;
using Depot.CleanArchitecture.Infrastructure.Identity;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using Depot.CleanArchitecture.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddPersistence(configuration);
        services.AddMultiTenancy();
        services.AddCachingServices();
        services.AddIdentityServices();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration.GetConnectionString("Database");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString) &&
                !connectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
            }
            else
            {
                options.UseInMemoryDatabase("DepotDb");
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    }

    private static void AddMultiTenancy(this IServiceCollection services)
    {
        services.AddScoped<ITenantProvider, TenantProvider>();
    }

    private static void AddCachingServices(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            };
        });

        services.AddSingleton<ICacheService, HybridCacheService>();
    }

    private static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, CurrentUser>();
    }
}
