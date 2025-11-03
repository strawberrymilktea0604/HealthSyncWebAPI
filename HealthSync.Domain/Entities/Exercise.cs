using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class Exercise
{
    public int ExerciseId { get; set; }

    // Basic info
    public string Name { get; set; } = null!;
    public MuscleGroup MuscleGroup { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public Equipment? Equipment { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal? CaloriesPerMinute { get; set; }

    // Admin tracking
    public int CreatedByAdminId { get; set; }
    public ApplicationUser CreatedByAdmin { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<ExerciseSession> ExerciseSessions { get; set; } = new List<ExerciseSession>();
}