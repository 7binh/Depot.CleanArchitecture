using Depot.Application.Abstractions;
using Depot.Application.Abstractions.Data;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Infrastructure.Identity;
using Depot.Domain.Yard;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depot.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IdentityUserContext<ApplicationUser, Guid>(options), IAppDbContext
{
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();

    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Block> Blocks => Set<Block>();

    public DbSet<Bay> Bays => Set<Bay>();

    public DbSet<YardRow> YardRows => Set<YardRow>();

    public DbSet<Container> Containers => Set<Container>();

    public DbSet<ReleaseOrder> ReleaseOrders => Set<ReleaseOrder>();

    public DbSet<ContainerStay> ContainerStays => Set<ContainerStay>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FillAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task LockReleaseOrderAsync(Guid releaseOrderId, CancellationToken cancellationToken = default)
    {
        // Row locks are a PostgreSQL feature; the in-memory provider used by the tests has nothing to lock.
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM "ReleaseOrders" WHERE "Id" = {releaseOrderId} FOR UPDATE""",
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private void FillAuditFields()
    {
        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedAt).CurrentValue = now;
                entry.Property(x => x.CreatedBy).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.LastModifiedAt).CurrentValue = now;
                entry.Property(x => x.LastModifiedBy).CurrentValue = userId;
            }
        }
    }
}
