namespace HealthSync.Domain.Entities;

public class ExerciseSession
{
    public int ExerciseSessionId { get; set; }

    // Parent workout
    public int WorkoutLogId { get; set; }
    public WorkoutLog WorkoutLog { get; set; } = null!;

    // Exercise reference
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    // Performance data
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public int? RestSeconds { get; set; }
    public int? Rpe { get; set; } // Rate of Perceived Exertion (1-10)
    public int? DurationMinutes { get; set; } // For cardio exercises
    public string? Notes { get; set; }
    public int OrderIndex { get; set; } // Order in workout
}