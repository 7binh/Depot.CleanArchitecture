namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using System.Text.Json;
using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ConditionSurveyConfiguration : IEntityTypeConfiguration<ConditionSurvey>
{
    public void Configure(EntityTypeBuilder<ConditionSurvey> builder)
    {
        builder.ToTable("ConditionSurveys");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Condition)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.PhotoRefs)
            .HasMaxLength(2000);

        builder.Property(s => s.InspectedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.DamageCodes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("text");

        builder.HasIndex(s => new { s.TenantId, s.VisitId, s.InspectedAt });
    }
}
