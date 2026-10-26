using Depot.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class ContainerTypeConfiguration : IEntityTypeConfiguration<ContainerType>
{
    // Fixed on purpose: seed data must be identical every time the migration is generated.
    private static readonly DateTimeOffset _seedCreatedAt = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<ContainerType> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(ContainerType.CodeMaxLength);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(ContainerType.NameMaxLength);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new { Id = new Guid("5b0c6f10-0001-4a5e-9a10-000000000001"), Code = "DRY", Name = "Dry", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0002-4a5e-9a10-000000000002"), Code = "REEFER", Name = "Reefer", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0003-4a5e-9a10-000000000003"), Code = "OPENTOP", Name = "Open Top", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0004-4a5e-9a10-000000000004"), Code = "FLATRACK", Name = "Flat Rack", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0005-4a5e-9a10-000000000005"), Code = "BUNKER", Name = "Bunker", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0006-4a5e-9a10-000000000006"), Code = "VENTILATED", Name = "Ventilated", CreatedAt = _seedCreatedAt },
            new { Id = new Guid("5b0c6f10-0007-4a5e-9a10-000000000007"), Code = "SPECIALIZED", Name = "Specialized", CreatedAt = _seedCreatedAt });
    }
}
