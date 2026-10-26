using Depot.Domain.Catalogs;
using Depot.Domain.ReleaseOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class ReleaseOrderItemConfiguration : IEntityTypeConfiguration<ReleaseOrderItem>
{
    public void Configure(EntityTypeBuilder<ReleaseOrderItem> builder)
    {
        builder.ToTable("ReleaseOrderItems");

        builder.HasKey(x => x.Id);

        builder.HasOne<ContainerType>()
            .WithMany()
            .HasForeignKey(x => x.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ReleaseOrderId, x.ContainerTypeId, x.SizeFeet }).IsUnique();
    }
}
