using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class ChallengeParticipationConfiguration : IEntityTypeConfiguration<ChallengeParticipation>
{
    public void Configure(EntityTypeBuilder<ChallengeParticipation> builder)
    {
        builder.HasKey(cp => cp.ParticipationId);

        builder.Property(cp => cp.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cp => cp.SubmissionText)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(cp => cp.SubmissionUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(cp => cp.ReviewNotes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(cp => cp.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // Relationships
        builder.HasOne(cp => cp.Challenge)
            .WithMany(c => c.Participations)
            .HasForeignKey(cp => cp.ChallengeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cp => cp.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(cp => cp.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(cp => cp.ChallengeId);
        builder.HasIndex(cp => cp.UserId);
        builder.HasIndex(cp => new { cp.ChallengeId, cp.UserId }).IsUnique();
        builder.HasIndex(cp => cp.Status);
        builder.HasIndex(cp => cp.ReviewedByAdminId);
    }
}