using System;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.WorkoutLogs;

public class CreateExerciseSessionRequest
{
    [Required(ErrorMessage = "Exercise ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Exercise ID must be valid")]
    public int ExerciseId { get; set; }

    [Required(ErrorMessage = "Sets is required")]
    [Range(1, 1000, ErrorMessage = "Sets must be between 1 and 1000")]
    public int Sets { get; set; }

    [Required(ErrorMessage = "Reps is required")]
    [Range(1, 1000, ErrorMessage = "Reps must be between 1 and 1000")]
    public int Reps { get; set; }

    [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000 kg")]
    public decimal? WeightKg { get; set; }

    [Range(0, 600, ErrorMessage = "Rest duration must be between 0 and 600 seconds")]
    public int? RestSeconds { get; set; }

    [Range(1, 10, ErrorMessage = "RPE must be between 1 and 10")]
    public int? Rpe { get; set; }

    [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes")]
    public int? DurationMinutes { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "OrderIndex is required")]
    [Range(0, int.MaxValue, ErrorMessage = "OrderIndex must be >= 0")]
    public int OrderIndex { get; set; }
}