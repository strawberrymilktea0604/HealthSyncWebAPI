using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.HasKey(c => c.ChallengeId);

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.ChallengeType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Criteria)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.MaxParticipants)
            .IsRequired(false);

        builder.Property(c => c.RewardDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // Relationships
        builder.HasOne(c => c.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(c => c.CreatedByAdminId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.Participations)
            .WithOne(cp => cp.Challenge)
            .HasForeignKey(cp => cp.ChallengeId);

        // Indexes
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedByAdminId);
        builder.HasIndex(c => c.StartDate);
        builder.HasIndex(c => c.EndDate);
    }
}