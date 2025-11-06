using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ProgressRecordConfiguration : IEntityTypeConfiguration<ProgressRecord>
{
    public void Configure(EntityTypeBuilder<ProgressRecord> builder)
    {
        builder.HasKey(pr => pr.ProgressRecordId);

        builder.Property(pr => pr.RecordedValue)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.WeightKg)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.WaistCm)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.ChestCm)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.HipCm)
            .HasPrecision(18, 2);

        builder.Property(pr => pr.Notes)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(pr => pr.Goal)
            .WithMany(g => g.ProgressRecords)
            .HasForeignKey(pr => pr.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pr => pr.GoalId);
        builder.HasIndex(pr => new { pr.GoalId, pr.RecordDate });
    }
}