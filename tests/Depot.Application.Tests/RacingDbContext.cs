using Depot.Application.Abstractions.Data;
using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Domain.Yard;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depot.Application.Tests;

/// <summary>
/// Mô phỏng race: ngay trước khi lưu, một "người khác" (<c>rivalSave</c>) đã lưu xong cùng khóa,
/// nên lần lưu của handler bị database từ chối như khi vi phạm unique index.
/// </summary>
internal sealed class RacingDbContext(
    AppDbContext inner,
    Func<CancellationToken, Task> rivalSave,
    Func<DbUpdateException>? failure = null) : IAppDbContext
{
    public DbSet<LineOperator> LineOperators => inner.LineOperators;

    public DbSet<ContainerType> ContainerTypes => inner.ContainerTypes;

    public DbSet<Customer> Customers => inner.Customers;

    public DbSet<Block> Blocks => inner.Blocks;

    public DbSet<Bay> Bays => inner.Bays;

    public DbSet<YardRow> YardRows => inner.YardRows;

    public DbSet<Container> Containers => inner.Containers;

    public DbSet<ReleaseOrder> ReleaseOrders => inner.ReleaseOrders;

    public DbSet<ContainerStay> ContainerStays => inner.ContainerStays;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await rivalSave(cancellationToken);

        throw failure?.Invoke() ?? new DbUpdateException("Simulated unique index violation.");
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return inner.BeginTransactionAsync(cancellationToken);
    }

    public Task LockReleaseOrderAsync(Guid releaseOrderId, CancellationToken cancellationToken = default)
    {
        return inner.LockReleaseOrderAsync(releaseOrderId, cancellationToken);
    }
}
