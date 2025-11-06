using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(g => g.GoalId);

        builder.Property(g => g.GoalType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(g => g.TargetValue)
            .HasPrecision(18, 2);

        builder.Property(g => g.Unit)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<string>();

        // Relationships
        builder.HasOne(g => g.User)
            .WithMany(u => u.Goals)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.ProgressRecords)
            .WithOne(pr => pr.Goal)
            .HasForeignKey(pr => pr.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(g => g.UserId);
    }
}