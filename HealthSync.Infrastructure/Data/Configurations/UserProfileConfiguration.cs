using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(up => up.UserProfileId);

        builder.Property(up => up.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(up => up.Gender)
            .HasMaxLength(20);

        builder.Property(up => up.ActivityLevel)
            .HasMaxLength(50);

        builder.Property(up => up.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.InitialHeightCm)
            .HasPrecision(18, 2);

        builder.Property(u => u.InitialWeightKg)
            .HasPrecision(18, 2);

        builder.Property(u => u.CurrentHeightCm)
            .HasPrecision(18, 2);

        builder.Property(u => u.CurrentWeightKg)
            .HasPrecision(18, 2);

        // 1-to-1 relationship with ApplicationUser
        builder.HasOne(up => up.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}