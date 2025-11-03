using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.ExerciseId);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.MuscleGroup)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.DifficultyLevel)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Equipment)
            .HasConversion<string>();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Instructions)
            .HasMaxLength(2000);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.CaloriesPerMinute)
            .HasPrecision(5, 2);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(e => e.CreatedByAdmin)
            .WithMany() // Admin can create many exercises
            .HasForeignKey(e => e.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.MuscleGroup);
        builder.HasIndex(e => e.CreatedByAdminId);
    }
}