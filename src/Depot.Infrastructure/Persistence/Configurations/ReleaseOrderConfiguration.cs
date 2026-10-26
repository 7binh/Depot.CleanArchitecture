using Depot.Domain.Catalogs;
using Depot.Domain.ReleaseOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class ReleaseOrderConfiguration : IEntityTypeConfiguration<ReleaseOrder>
{
    public void Configure(EntityTypeBuilder<ReleaseOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNo)
            .IsRequired()
            .HasMaxLength(ReleaseOrder.OrderNoMaxLength);

        builder.Property(x => x.ExportVessel)
            .IsRequired()
            .HasMaxLength(ReleaseOrder.ExportVesselMaxLength);

        builder.HasOne<LineOperator>()
            .WithMany()
            .HasForeignKey(x => x.LineOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ReleaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.LineOperatorId, x.OrderNo }).IsUnique();
    }
}
