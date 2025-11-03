using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace HealthSync.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    // Additional properties
    public string Role { get; set; } = null!; // Customer, Admin, TopContributor
    public bool IsActive { get; set; } = true;
    public string? OauthProvider { get; set; } // null | Google | Facebook
    public string? OauthProviderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation properties
    public UserProfile? UserProfile { get; set; }
    public Leaderboard? Leaderboard { get; set; }
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
    public ICollection<NutritionLog> NutritionLogs { get; set; } = new List<NutritionLog>();
    public ICollection<ChallengeParticipation> ChallengeParticipations { get; set; } = new List<ChallengeParticipation>();
}