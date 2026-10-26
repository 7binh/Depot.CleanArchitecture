using Depot.Application.Abstractions;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Depot.Application.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        return Create(Guid.NewGuid().ToString());
    }

    public static AppDbContext Create(string databaseName)
    {
        return Create(databaseName, new FakeCurrentUser(null), TimeProvider.System);
    }

    public static AppDbContext Create(string databaseName, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, currentUser, timeProvider);
    }
}
