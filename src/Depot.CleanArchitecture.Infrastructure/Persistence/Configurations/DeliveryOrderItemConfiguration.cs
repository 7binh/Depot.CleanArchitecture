namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class DeliveryOrderItemConfiguration : IEntityTypeConfiguration<DeliveryOrderItem>
{
    public void Configure(EntityTypeBuilder<DeliveryOrderItem> builder)
    {
        builder.ToTable("DeliveryOrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.DeliveryOrderId)
            .IsRequired();

        builder.HasIndex(i => i.DeliveryOrderId);
    }
}
