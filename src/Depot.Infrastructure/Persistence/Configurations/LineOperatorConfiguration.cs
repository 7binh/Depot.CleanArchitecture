using Depot.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class LineOperatorConfiguration : IEntityTypeConfiguration<LineOperator>
{
    public void Configure(EntityTypeBuilder<LineOperator> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(LineOperator.CodeLength);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(LineOperator.NameMaxLength);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
