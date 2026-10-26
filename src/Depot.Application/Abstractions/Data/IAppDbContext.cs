using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks one release order until the current transaction ends, so gate outs on the same order run one at a time.
    /// </summary>
    Task LockReleaseOrderAsync(Guid releaseOrderId, CancellationToken cancellationToken = default);
}
