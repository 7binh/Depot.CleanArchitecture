namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class YardSlotConfiguration : IEntityTypeConfiguration<YardSlot>
{
    public void Configure(EntityTypeBuilder<YardSlot> builder)
    {
        builder.ToTable("YardSlots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BlockId)
            .IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.BlockId, s.Bay, s.Row, s.Tier })
            .IsUnique();

        builder.HasIndex(s => new { s.TenantId, s.IsOccupied });
    }
}
