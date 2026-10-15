namespace Depot.CleanArchitecture.Infrastructure.Persistence;

using System.Reflection;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantProvider? tenantProvider = null)
    : DbContext(options), IAppDbContext
{
    public DbSet<YardBlock> YardBlocks => Set<YardBlock>();
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerVisit> ContainerVisits => Set<ContainerVisit>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<ConditionSurvey> ConditionSurveys => Set<ConditionSurvey>();
    public DbSet<StayHold> StayHolds => Set<StayHold>();
    public DbSet<MovementEvent> MovementEvents => Set<MovementEvent>();

    public Guid CurrentTenantId => tenantProvider?.CurrentTenantId ?? TenantInfo.DefaultDepotCatLaiId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Multi-tenant global query filter
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplyTenantFilter),
                        BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, [builder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        builder.Entity<TEntity>().HasIndex(e => e.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        SetTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = utcNow;
                    break;
            }
        }
    }

    private void SetTenantId()
    {
        var tenantId = CurrentTenantId;
        var tenantEntries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty);

        foreach (var entry in tenantEntries)
        {
            entry.Entity.TenantId = tenantId;
        }
    }
}
