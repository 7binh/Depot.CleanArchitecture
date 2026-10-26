using Depot.Domain.Containers;
using Depot.Domain.Gate;
using Depot.Domain.ReleaseOrders;
using Depot.Domain.Yard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class ContainerStayConfiguration : IEntityTypeConfiguration<ContainerStay>
{
    // Longest enum name is "Damaged"/"Truck"; 10 leaves room without wasting a wide column.
    private const int _enumNameLength = 10;
    private const string _openStayFilter = "\"GateOutAt\" IS NULL";

    public void Configure(EntityTypeBuilder<ContainerStay> builder)
    {
        builder.HasKey(x => x.Id);

        // PostgreSQL's own row version: saving a stay someone else changed meanwhile fails instead of overwriting it.
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        // Stored as text so the SQL statistics (database/queries) read "Damaged", not 1.
        builder.Property(x => x.Grade).HasConversion<string>().HasMaxLength(_enumNameLength);
        builder.Property(x => x.Condition).HasConversion<string>().HasMaxLength(_enumNameLength);
        builder.Property(x => x.InVehicleType).HasConversion<string>().HasMaxLength(_enumNameLength);

        builder.Property(x => x.OutVehicleType).HasConversion<string>().HasMaxLength(_enumNameLength);

        builder.Property(x => x.DamageNote).HasMaxLength(ContainerStay.DamageNoteMaxLength);
        builder.Property(x => x.InVehicleNumber)
            .IsRequired()
            .HasMaxLength(ContainerStay.VehicleNumberMaxLength);
        builder.Property(x => x.OutVehicleNumber).HasMaxLength(ContainerStay.VehicleNumberMaxLength);

        builder.HasOne<Container>()
            .WithMany()
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Block>()
            .WithMany()
            .HasForeignKey(x => x.BlockId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict also guards open stays: a row cannot be deleted while a container stands in it (BR-YD-09). The
        // delete handlers first clear RowId on closed stays, so a row that only has history can still go.
        builder.HasOne<YardRow>()
            .WithMany()
            .HasForeignKey(x => x.RowId)
            .OnDelete(DeleteBehavior.Restrict);

        // The line a container left on; the order is reached through the line (BR-GO-02).
        builder.HasOne<ReleaseOrderItem>()
            .WithMany()
            .HasForeignKey(x => x.ReleaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique only among open stays (ERD): a container may come back after it left (BR-GI-04), and a position is
        // free again once its container left (BR-GO-09). PostgreSQL treats NULLs as distinct, so stays in a virtual
        // block (no row, no tier) never collide.
        builder.HasIndex(x => x.ContainerId)
            .IsUnique()
            .HasFilter(_openStayFilter);
        builder.HasIndex(x => new { x.RowId, x.Tier })
            .IsUnique()
            .HasFilter(_openStayFilter);

        builder.HasIndex(x => x.ReleaseOrderItemId);
    }
}
