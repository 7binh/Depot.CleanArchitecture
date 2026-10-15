namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Containers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Number)
            .HasConversion(
                n => n.Value,
                v => ContainerNumber.Create(v).Value!)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(c => c.LineOperator)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.IsoCode)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(c => c.TareWeight)
            .HasPrecision(18, 2);

        builder.Property(c => c.MaxGrossWeight)
            .HasPrecision(18, 2);

        builder.Property(c => c.DamageNotes)
            .HasMaxLength(500);

        builder.HasIndex(c => new { c.TenantId, c.Number })
            .IsUnique();
    }
}
