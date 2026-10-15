namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MovementEventConfiguration : IEntityTypeConfiguration<MovementEvent>
{
    public void Configure(EntityTypeBuilder<MovementEvent> builder)
    {
        builder.ToTable("MovementEvents");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ActorId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Reason)
            .HasMaxLength(500);

        builder.HasIndex(m => new { m.TenantId, m.VisitId, m.OccurredAt });
    }
}
