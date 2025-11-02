using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthSync.Infrastructure.Data.Configurations;

public class LeaderboardConfiguration : IEntityTypeConfiguration<Leaderboard>
{
    public void Configure(EntityTypeBuilder<Leaderboard> builder)
    {
        builder.HasKey(l => l.LeaderboardId);

        builder.Property(l => l.TotalPoints)
            .HasDefaultValue(0);

        builder.Property(l => l.RankTitle)
            .HasMaxLength(100);

        builder.HasOne(l => l.User)
            .WithOne(u => u.Leaderboard)
            .HasForeignKey<Leaderboard>(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}