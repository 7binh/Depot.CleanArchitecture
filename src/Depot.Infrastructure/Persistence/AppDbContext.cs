using Depot.Application.Abstractions;
using Depot.Application.Abstractions.Data;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : DbContext(options), IAppDbContext
{
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();

    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Block> Blocks => Set<Block>();

    public DbSet<Bay> Bays => Set<Bay>();

    public DbSet<YardRow> YardRows => Set<YardRow>();

    public DbSet<Container> Containers => Set<Container>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FillAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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
