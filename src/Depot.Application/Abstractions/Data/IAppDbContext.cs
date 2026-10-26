using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<LineOperator> LineOperators { get; }

    DbSet<ContainerType> ContainerTypes { get; }

    DbSet<Customer> Customers { get; }

    DbSet<Block> Blocks { get; }

    DbSet<Bay> Bays { get; }

    DbSet<YardRow> YardRows { get; }

    DbSet<Container> Containers { get; }

    DbSet<ReleaseOrder> ReleaseOrders { get; }

    DbSet<ContainerStay> ContainerStays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
