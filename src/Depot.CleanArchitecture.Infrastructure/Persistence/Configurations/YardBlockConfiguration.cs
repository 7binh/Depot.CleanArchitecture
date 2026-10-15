namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class YardBlockConfiguration : IEntityTypeConfiguration<YardBlock>
{
    public void Configure(EntityTypeBuilder<YardBlock> builder)
    {
        builder.ToTable("YardBlocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlockCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(b => new { b.TenantId, b.BlockCode })
            .IsUnique();

        builder.HasMany(b => b.Slots)
            .WithOne()
            .HasForeignKey(s => s.BlockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
