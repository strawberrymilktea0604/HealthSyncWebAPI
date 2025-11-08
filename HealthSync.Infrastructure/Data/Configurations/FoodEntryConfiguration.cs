using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class FoodEntryConfiguration : IEntityTypeConfiguration<FoodEntry>
{
    public void Configure(EntityTypeBuilder<FoodEntry> builder)
    {
        builder.HasKey(fe => fe.FoodEntryId);

        builder.Property(fe => fe.Quantity)
            .HasPrecision(6, 2);

        builder.Property(fe => fe.MealType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(fe => fe.Calories)
            .HasPrecision(8, 2);

        builder.Property(fe => fe.ProteinG)
            .HasPrecision(6, 2);

        builder.Property(fe => fe.CarbsG)
            .HasPrecision(6, 2);

        builder.Property(fe => fe.FatG)
            .HasPrecision(6, 2);

        builder.Property(fe => fe.Notes)
            .HasMaxLength(500);

        // Foreign key relationships
        builder.HasOne(fe => fe.NutritionLog)
            .WithMany(n => n.FoodEntries)
            .HasForeignKey(fe => fe.NutritionLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fe => fe.FoodItem)
            .WithMany(f => f.FoodEntries)
            .HasForeignKey(fe => fe.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(fe => fe.NutritionLogId);
        builder.HasIndex(fe => fe.FoodItemId);
        builder.HasIndex(fe => new { fe.NutritionLogId, fe.MealType });
    }
}