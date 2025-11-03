using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ExerciseSessionConfiguration : IEntityTypeConfiguration<ExerciseSession>
{
    public void Configure(EntityTypeBuilder<ExerciseSession> builder)
    {
        builder.HasKey(es => es.ExerciseSessionId);

        builder.Property(es => es.Sets)
            .IsRequired();

        builder.Property(es => es.Reps)
            .IsRequired();

        builder.Property(es => es.WeightKg)
            .HasPrecision(6, 2);

        builder.Property(es => es.RestSeconds)
            .IsRequired(false);

        builder.Property(es => es.Rpe)
            .IsRequired(false);

        builder.Property(es => es.DurationMinutes)
            .IsRequired(false);

        builder.Property(es => es.Notes)
            .HasMaxLength(500);

        builder.Property(es => es.OrderIndex)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne(es => es.WorkoutLog)
            .WithMany(w => w.ExerciseSessions)
            .HasForeignKey(es => es.WorkoutLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Exercise)
            .WithMany(e => e.ExerciseSessions)
            .HasForeignKey(es => es.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(es => es.WorkoutLogId);
        builder.HasIndex(es => es.ExerciseId);
        builder.HasIndex(es => new { es.WorkoutLogId, es.OrderIndex });
    }
}