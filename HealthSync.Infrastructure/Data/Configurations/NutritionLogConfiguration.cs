using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class NutritionLogConfiguration : IEntityTypeConfiguration<NutritionLog>
{
    public void Configure(EntityTypeBuilder<NutritionLog> builder)
    {
        builder.Property(n => n.TotalCalories)
            .HasPrecision(18, 2);
    }
}