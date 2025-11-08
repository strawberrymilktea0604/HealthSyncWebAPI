using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

/// <summary>
/// Application User entity - Authentication and authorization
/// Không sử dụng Identity để tối ưu hóa database schema
/// </summary>
public class ApplicationUser
{
    // Primary Key
    public int UserId { get; set; }

    // Authentication fields
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Authorization
    public string Role { get; set; } = null!; // Customer, Admin, TopContributor

    // Account status
    public bool IsActive { get; set; } = true;

    // OAuth2 Social Login
    public string? OauthProvider { get; set; } // null | Google | Facebook
    public string? OauthProviderId { get; set; }

    // Refresh Token (JWT)
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public UserProfile? UserProfile { get; set; }
    public Leaderboard? Leaderboard { get; set; }
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
    public ICollection<NutritionLog> NutritionLogs { get; set; } = new List<NutritionLog>();
    public ICollection<ChallengeParticipation> ChallengeParticipations { get; set; } = new List<ChallengeParticipation>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Reply> Replies { get; set; } = new List<Reply>();
}