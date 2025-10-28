using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ProgressRecordConfiguration : IEntityTypeConfiguration<ProgressRecord>
{
    public void Configure(EntityTypeBuilder<ProgressRecord> builder)
    {
        builder.Property(p => p.RecordValue)
            .HasPrecision(18, 2);

        builder.Property(p => p.Weight)
            .HasPrecision(18, 2);

        builder.Property(p => p.WaistCm)
            .HasPrecision(18, 2);
    }
}