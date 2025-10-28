using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class Exercise
{
    public int ExerciseId { get; set; }

    // Basic info
    public string Name { get; set; } = null!;
    public string MuscleGroup { get; set; } = null!;
    public string Difficulty { get; set; } = null!; // Beginner, Intermediate, Advanced
    public string? Equipment { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation properties
    public ICollection<ExerciseSession> ExerciseSessions { get; set; } = new List<ExerciseSession>();
}