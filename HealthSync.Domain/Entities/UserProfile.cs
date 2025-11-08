using System;

namespace HealthSync.Domain.Entities;

public class UserProfile
{
    public int UserProfileId { get; set; }

    // User reference (1-to-1)
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Personal info
    public string FullName { get; set; } = null!;
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal? HeightCm { get; set; } // Current height in cm
    public decimal? CurrentWeightKg { get; set; } // Current weight
    public ActivityLevel? ActivityLevel { get; set; } // Sedentary, LightlyActive, ModeratelyActive, VeryActive, ExtraActive
    public string? AvatarUrl { get; set; }
    public int ContributionPoints { get; set; } = 0; // Points for forum + challenges
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}