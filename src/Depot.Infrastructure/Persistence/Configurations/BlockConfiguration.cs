using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(Block.CodeMaxLength);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Block.NameMaxLength);

        builder.Property(x => x.IsVirtual).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
