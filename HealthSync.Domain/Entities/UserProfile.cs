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
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal? InitialHeightCm { get; set; } // Initial height in cm
    public decimal? InitialWeightKg { get; set; } // Initial weight in kg
    public decimal? CurrentHeightCm { get; set; } // Current height if changed
    public decimal? CurrentWeightKg { get; set; } // Current weight
    public string? ActivityLevel { get; set; } // Sedentary, Light, Moderate, Active, Very Active
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}