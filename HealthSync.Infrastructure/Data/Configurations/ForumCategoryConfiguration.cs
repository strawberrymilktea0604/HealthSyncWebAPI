using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> builder)
    {
        builder.HasKey(fc => fc.CategoryId);

        builder.Property(fc => fc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fc => fc.Description)
            .HasMaxLength(500);

        builder.Property(fc => fc.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(fc => fc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(fc => fc.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint on Name
        builder.HasIndex(fc => fc.Name)
            .IsUnique();

        // Relationships
        builder.HasMany(fc => fc.Posts)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}