namespace Depot.CleanArchitecture.Application.UnitTests;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public class InMemoryAppDbContext : DbContext, IAppDbContext
{
    public InMemoryAppDbContext(DbContextOptions<InMemoryAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<YardBlock> YardBlocks => Set<YardBlock>();
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerVisit> ContainerVisits => Set<ContainerVisit>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<ConditionSurvey> ConditionSurveys => Set<ConditionSurvey>();
    public DbSet<StayHold> StayHolds => Set<StayHold>();
    public DbSet<MovementEvent> MovementEvents => Set<MovementEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeliveryOrder>().HasMany(d => d.Items);

        modelBuilder.Entity<Container>(b =>
        {
            b.Property(c => c.Number)
                .HasConversion(n => n.Value, v => ContainerNumber.Create(v).Value!);
        });

        modelBuilder.Entity<ContainerVisit>(b =>
        {
            b.OwnsOne(v => v.InVehicle);
            b.OwnsOne(v => v.OutVehicle);
            b.HasMany(v => v.Surveys).WithOne().HasForeignKey(s => s.VisitId);
            b.HasMany(v => v.Holds).WithOne().HasForeignKey(h => h.VisitId);
            b.HasMany(v => v.Movements).WithOne().HasForeignKey(m => m.VisitId);
            b.Property(v => v.CurrentSlotId).IsConcurrencyToken();
            b.Property(v => v.CurrentBlockId).IsConcurrencyToken();
        });

        modelBuilder.Entity<YardSlot>(b =>
        {
            b.Property(s => s.IsOccupied).IsConcurrencyToken();
        });
    }
}

public static class TestDbContextFactory
{
    public static InMemoryAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<InMemoryAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InMemoryAppDbContext(options);
    }
}
