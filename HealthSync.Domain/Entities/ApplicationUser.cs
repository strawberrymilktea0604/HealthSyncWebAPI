using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class ApplicationUser
{
    public int UserId { get; set; }

    // Authentication
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
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