using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.Property(f => f.ServingSize)
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