using Depot.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depot.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaxCode)
            .IsRequired()
            .HasMaxLength(Customer.LongTaxCodeLength);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Customer.NameMaxLength);

        builder.HasIndex(x => x.TaxCode).IsUnique();
    }
}
