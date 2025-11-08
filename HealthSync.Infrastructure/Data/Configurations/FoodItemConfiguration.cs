using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.HasKey(f => f.FoodItemId);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(f => f.Description)
            .HasMaxLength(1000);

        builder.Property(f => f.ImageUrl)
            .HasMaxLength(500);

        builder.Property(f => f.ServingSize)
            .HasPrecision(8, 2);

        builder.Property(f => f.ServingUnit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.CaloriesPerServing)
            .HasPrecision(8, 2);

        builder.Property(f => f.ProteinG)
            .HasPrecision(6, 2);

        builder.Property(f => f.CarbsG)
            .HasPrecision(6, 2);

        builder.Property(f => f.FatG)
            .HasPrecision(6, 2);

        builder.Property(f => f.FiberG)
            .HasPrecision(6, 2);

        builder.Property(f => f.SugarG)
            .HasPrecision(6, 2);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(f => f.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(f => f.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(f => f.Name);
        builder.HasIndex(f => f.Category);
        builder.HasIndex(f => f.CreatedByAdminId);
    }
}