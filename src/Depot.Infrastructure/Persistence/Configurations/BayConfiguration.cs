using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class BayConfiguration : IEntityTypeConfiguration<Bay>
{
    public void Configure(EntityTypeBuilder<Bay> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BayNo).IsRequired();

        builder.HasOne<Block>()
            .WithMany()
            .HasForeignKey(x => x.BlockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BlockId, x.BayNo }).IsUnique();
    }
}
