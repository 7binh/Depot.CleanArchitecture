namespace Depot.CleanArchitecture.Application.Abstractions.Data;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IAppDbContext
{
    DbSet<YardBlock> YardBlocks { get; }
    DbSet<YardSlot> YardSlots { get; }
    DbSet<Container> Containers { get; }
    DbSet<ContainerVisit> ContainerVisits { get; }
    DbSet<DeliveryOrder> DeliveryOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
