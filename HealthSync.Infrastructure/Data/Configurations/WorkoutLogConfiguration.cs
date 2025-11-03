using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.HasKey(w => w.WorkoutLogId);

        builder.Property(w => w.WorkoutDate)
            .IsRequired();

        builder.Property(w => w.TotalDurationMinutes)
            .IsRequired();

        builder.Property(w => w.EstimatedCaloriesBurned)
            .HasPrecision(8, 2);

        builder.Property(w => w.Notes)
            .HasMaxLength(1000);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(w => w.UserId);
        builder.HasIndex(w => w.WorkoutDate);
        builder.HasIndex(w => new { w.UserId, w.WorkoutDate });
    }
}