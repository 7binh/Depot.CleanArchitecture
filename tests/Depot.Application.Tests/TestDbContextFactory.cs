using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        return Create(Guid.NewGuid().ToString());
    }

    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }
}
