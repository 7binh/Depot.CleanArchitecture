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
    DbSet<ConditionSurvey> ConditionSurveys { get; }
    DbSet<StayHold> StayHolds { get; }
    DbSet<MovementEvent> MovementEvents { get; }

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
