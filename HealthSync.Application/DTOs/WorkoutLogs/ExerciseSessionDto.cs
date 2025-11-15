using System;

namespace HealthSync.Application.DTOs.WorkoutLogs;

public class ExerciseSessionDto
{
    public int ExerciseSessionId { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public int? RestSeconds { get; set; }
    public int? Rpe { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public int OrderIndex { get; set; }
}