using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.WorkoutLogs;

public class CreateWorkoutLogRequest
{
    [Required(ErrorMessage = "Workout date is required")]
    public DateTime WorkoutDate { get; set; }

    [Required(ErrorMessage = "Total duration is required")]
    [Range(1, 1440, ErrorMessage = "Total duration must be between 1 and 1440 minutes")]
    public int TotalDurationMinutes { get; set; }

    [Required(ErrorMessage = "Estimated calories is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Estimated calories must be >= 0")]
    public decimal EstimatedCaloriesBurned { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "Exercise sessions are required")]
    [MinLength(1, ErrorMessage = "At least 1 exercise session is required")]
    public List<CreateExerciseSessionRequest> ExerciseSessions { get; set; } = new();
}