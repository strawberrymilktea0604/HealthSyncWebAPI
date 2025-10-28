using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class UserChallengeSubmissionConfiguration : IEntityTypeConfiguration<UserChallengeSubmission>
{
    public void Configure(EntityTypeBuilder<UserChallengeSubmission> builder)
    {
        builder.Property(u => u.CurrentProgress)
            .HasPrecision(18, 2);

        builder.Property(u => u.TargetProgress)
            .HasPrecision(18, 2);

        builder.Property(u => u.ProgressPercentage)
            .HasPrecision(18, 2);

        builder.Property(u => u.EarnedPoints)
            .HasPrecision(18, 2);
    }
}