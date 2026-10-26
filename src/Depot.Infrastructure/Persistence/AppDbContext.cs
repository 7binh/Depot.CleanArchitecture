using Depot.Application.Abstractions.Data;
using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();

    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Block> Blocks => Set<Block>();

    public DbSet<Bay> Bays => Set<Bay>();

    public DbSet<YardRow> YardRows => Set<YardRow>();

    public DbSet<Container> Containers => Set<Container>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
