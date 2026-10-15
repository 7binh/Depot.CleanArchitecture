namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class StayHoldConfiguration : IEntityTypeConfiguration<StayHold>
{
    public void Configure(EntityTypeBuilder<StayHold> builder)
    {
        builder.ToTable("StayHolds");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.HoldCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(h => h.HeldBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.ReleasedBy)
            .HasMaxLength(100);

        builder.Property(h => h.ReleaseReason)
            .HasMaxLength(500);

        builder.HasIndex(h => new { h.TenantId, h.VisitId, h.ReleasedAt });
    }
}
