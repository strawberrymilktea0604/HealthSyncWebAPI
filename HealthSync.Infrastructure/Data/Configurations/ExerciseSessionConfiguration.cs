using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ExerciseSessionConfiguration : IEntityTypeConfiguration<ExerciseSession>
{
    public void Configure(EntityTypeBuilder<ExerciseSession> builder)
    {
        builder.Property(e => e.WeightKg)
            .HasPrecision(18, 2);
    }
}