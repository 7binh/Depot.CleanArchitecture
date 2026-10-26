using Depot.Domain.Catalogs;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests.Persistence;

public sealed class AuditFieldsTests
{
    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset _createdTime = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _changedTime = new(2026, 10, 27, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_FillsCreatedFields_WhenEntityIsAdded()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(databaseName, "user-1", _createdTime);
        dbContext.LineOperators.Add(LineOperator.Create("CSQ", "Cosco").Value);

        await dbContext.SaveChangesAsync(_ct);

        var saved = dbContext.LineOperators.Single();
        Assert.Equal(_createdTime, saved.CreatedAt);
        Assert.Equal("user-1", saved.CreatedBy);
        Assert.Null(saved.LastModifiedAt);
        Assert.Null(saved.LastModifiedBy);
    }

    [Fact]
    public async Task SaveChanges_FillsModifiedFields_AndKeepsCreatedFields_WhenEntityIsChanged()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var creator = CreateContext(databaseName, "user-1", _createdTime))
        {
            creator.LineOperators.Add(LineOperator.Create("CSQ", "Cosco").Value);
            await creator.SaveChangesAsync(_ct);
        }

        await using var editor = CreateContext(databaseName, "user-2", _changedTime);
        var entity = editor.LineOperators.Single();
        editor.Entry(entity).Property(x => x.Name).CurrentValue = "Cosco Shipping";
        await editor.SaveChangesAsync(_ct);

        Assert.Equal(_createdTime, entity.CreatedAt);
        Assert.Equal("user-1", entity.CreatedBy);
        Assert.Equal(_changedTime, entity.LastModifiedAt);
        Assert.Equal("user-2", entity.LastModifiedBy);
    }

    [Fact]
    public async Task SaveChanges_LeavesUserNull_WhenThereIsNoCurrentUser()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(databaseName, null, _createdTime);
        dbContext.LineOperators.Add(LineOperator.Create("CSQ", "Cosco").Value);

        await dbContext.SaveChangesAsync(_ct);

        var saved = dbContext.LineOperators.Single();
        Assert.Equal(_createdTime, saved.CreatedAt);
        Assert.Null(saved.CreatedBy);
    }

    private static AppDbContext CreateContext(string databaseName, string? userId, DateTimeOffset now)
    {
        return TestDbContextFactory.Create(databaseName, new FakeCurrentUser(userId), new FixedTimeProvider(now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
