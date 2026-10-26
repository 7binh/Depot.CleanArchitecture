using Depot.Application.Abstractions;
using Depot.Infrastructure.Identity;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Depot.Application.Tests.Identity;

internal static class IdentityTestHost
{
    public static ServiceProvider Build(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? [])
            .Build();
        var databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser(null));
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<AppDbContext>();
        services.AddScoped<IdentityService>();
        return services.BuildServiceProvider();
    }
}
