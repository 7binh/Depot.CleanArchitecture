using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Depot.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    /// <summary>Applies the EF Core migrations that the database does not have yet, so a fresh database gets its tables.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
