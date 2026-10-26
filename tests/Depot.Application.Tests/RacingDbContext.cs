using Depot.Application.Abstractions.Data;
using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Depot.Domain.Yard;
using Depot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Tests;

/// <summary>
/// Mô phỏng race: ngay trước khi lưu, một "người khác" đã lưu xong cùng mã,
/// nên lần lưu của handler bị database từ chối như khi vi phạm unique index.
/// </summary>
internal sealed class RacingDbContext(AppDbContext inner, AppDbContext rival, LineOperator rivalEntity)
    : IAppDbContext
{
    public DbSet<LineOperator> LineOperators => inner.LineOperators;

    public DbSet<ContainerType> ContainerTypes => inner.ContainerTypes;

    public DbSet<Customer> Customers => inner.Customers;

    public DbSet<Block> Blocks => inner.Blocks;

    public DbSet<Bay> Bays => inner.Bays;

    public DbSet<YardRow> YardRows => inner.YardRows;

    public DbSet<Container> Containers => inner.Containers;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        rival.LineOperators.Add(rivalEntity);
        await rival.SaveChangesAsync(cancellationToken);

        throw new DbUpdateException("Simulated unique index violation.");
    }
}
