using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace HealthSync.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    // Additional properties
    public string Role { get; set; } = null!; // Customer, Admin
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public UserProfile? UserProfile { get; set; }
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
    public ICollection<NutritionLog> NutritionLogs { get; set; } = new List<NutritionLog>();
    public ICollection<ChallengeParticipation> ChallengeParticipations { get; set; } = new List<ChallengeParticipation>();
}