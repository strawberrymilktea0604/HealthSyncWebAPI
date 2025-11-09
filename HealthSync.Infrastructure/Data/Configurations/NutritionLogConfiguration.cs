using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class NutritionLogConfiguration : IEntityTypeConfiguration<NutritionLog>
{
    public void Configure(EntityTypeBuilder<NutritionLog> builder)
    {
        builder.HasKey(n => n.NutritionLogId);

        builder.Property(n => n.LogDate)
            .IsRequired();

        builder.Property(n => n.TotalCalories)
            .HasPrecision(8, 2);

        builder.Property(n => n.TotalProteinG)
            .HasPrecision(6, 2);

        builder.Property(n => n.TotalCarbsG)
            .HasPrecision(6, 2);

        builder.Property(n => n.TotalFatG)
            .HasPrecision(6, 2);

        builder.Property(n => n.Notes)
            .HasMaxLength(1000);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(n => n.User)
            .WithMany(u => u.NutritionLogs)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one log per user per date
        builder.HasIndex(n => new { n.UserId, n.LogDate })
            .IsUnique();

        // Indexes
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.LogDate);
    }
}