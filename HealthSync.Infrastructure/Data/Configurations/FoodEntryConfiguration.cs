using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class FoodEntryConfiguration : IEntityTypeConfiguration<FoodEntry>
{
    public void Configure(EntityTypeBuilder<FoodEntry> builder)
    {
        builder.Property(f => f.Quantity)
            .HasPrecision(18, 2);

        builder.Property(f => f.CaloriesKcal)
            .HasPrecision(18, 2);

        builder.Property(f => f.ProteinGrams)
            .HasPrecision(18, 2);

        builder.Property(f => f.CarbsGrams)
            .HasPrecision(18, 2);

        builder.Property(f => f.FatGrams)
            .HasPrecision(18, 2);
    }
}