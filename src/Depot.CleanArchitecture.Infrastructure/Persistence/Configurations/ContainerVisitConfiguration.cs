namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ContainerVisitConfiguration : IEntityTypeConfiguration<ContainerVisit>
{
    public void Configure(EntityTypeBuilder<ContainerVisit> builder)
    {
        builder.ToTable("ContainerVisits");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.LineOperator)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.InDamageNotes)
            .HasMaxLength(500);

        builder.OwnsOne(v => v.InVehicle, vh =>
        {
            vh.Property(x => x.TractorNo).HasColumnName("InVehicle_TractorNo").HasMaxLength(20).IsRequired();
            vh.Property(x => x.TrailerNo).HasColumnName("InVehicle_TrailerNo").HasMaxLength(20).IsRequired();
            vh.Property(x => x.DriverName).HasColumnName("InVehicle_DriverName").HasMaxLength(100).IsRequired();
            vh.Property(x => x.DriverPhone).HasColumnName("InVehicle_DriverPhone").HasMaxLength(20);
        });

        builder.OwnsOne(v => v.OutVehicle, vh =>
        {
            vh.Property(x => x.TractorNo).HasColumnName("OutVehicle_TractorNo").HasMaxLength(20);
            vh.Property(x => x.TrailerNo).HasColumnName("OutVehicle_TrailerNo").HasMaxLength(20);
            vh.Property(x => x.DriverName).HasColumnName("OutVehicle_DriverName").HasMaxLength(100);
            vh.Property(x => x.DriverPhone).HasColumnName("OutVehicle_DriverPhone").HasMaxLength(20);
        });

        builder.HasOne<Container>()
            .WithMany()
            .HasForeignKey(v => v.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<YardSlot>()
            .WithMany()
            .HasForeignKey(v => v.CurrentSlotId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<YardBlock>()
            .WithMany()
            .HasForeignKey(v => v.CurrentBlockId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<DeliveryOrder>()
            .WithMany()
            .HasForeignKey(v => v.DeliveryOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => new { v.TenantId, v.Status, v.GateInDate, v.GateOutDate });

        builder.HasMany(v => v.Surveys)
            .WithOne()
            .HasForeignKey(s => s.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Holds)
            .WithOne()
            .HasForeignKey(h => h.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Movements)
            .WithOne()
            .HasForeignKey(m => m.VisitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
