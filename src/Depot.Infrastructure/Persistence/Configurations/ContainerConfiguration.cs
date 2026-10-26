using Depot.Domain.Catalogs;
using Depot.Domain.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    private const int _weightPrecision = 10;
    private const int _weightScale = 2;

    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .IsRequired()
            .HasMaxLength(Container.NumberLength);

        builder.Property(x => x.IsoCode)
            .IsRequired()
            .HasMaxLength(Container.IsoCodeLength);

        builder.Property(x => x.MaxWeightKg).HasPrecision(_weightPrecision, _weightScale);
        builder.Property(x => x.TareWeightKg).HasPrecision(_weightPrecision, _weightScale);

        builder.HasOne<LineOperator>()
            .WithMany()
            .HasForeignKey(x => x.LineOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ContainerType>()
            .WithMany()
            .HasForeignKey(x => x.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Number).IsUnique();
    }
}
