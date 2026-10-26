using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class YardRowConfiguration : IEntityTypeConfiguration<YardRow>
{
    public void Configure(EntityTypeBuilder<YardRow> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowNo).IsRequired();

        builder.Property(x => x.MaxTier).IsRequired();

        builder.HasOne<Bay>()
            .WithMany()
            .HasForeignKey(x => x.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BayId, x.RowNo }).IsUnique();
    }
}
