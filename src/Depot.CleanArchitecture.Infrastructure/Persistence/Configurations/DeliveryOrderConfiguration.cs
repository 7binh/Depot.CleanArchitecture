namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("DeliveryOrders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.LineOperator)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.CustomerName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(o => o.CustomerTaxCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.VesselName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.VoyageNo)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber })
            .IsUnique();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
