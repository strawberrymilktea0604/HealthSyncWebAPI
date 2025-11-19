using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.WorkoutLogs;

public class CreateWorkoutLogRequest
{
    [Required(ErrorMessage = "Workout date is required")]
    public DateTime WorkoutDate { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "Exercise sessions are required")]
    [MinLength(1, ErrorMessage = "At least one exercise session is required")]
    public List<CreateExerciseSessionRequest> ExerciseSessions { get; set; } = new();
}