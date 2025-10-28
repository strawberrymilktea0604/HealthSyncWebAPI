using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class CommunityChallengeConfiguration : IEntityTypeConfiguration<CommunityChallenge>
{
    public void Configure(EntityTypeBuilder<CommunityChallenge> builder)
    {
        builder.Property(c => c.TargetValue)
            .HasPrecision(18, 2);

        builder.Property(c => c.RewardPoints)
            .HasPrecision(18, 2);
    }
}