using System;
using System.Collections.Generic;

namespace HealthSync.Application.DTOs.WorkoutLogs;

public class WorkoutLogResponse
{
    public int WorkoutLogId { get; set; }
    public int UserId { get; set; }
    public DateTime WorkoutDate { get; set; }
    public int TotalDurationMinutes { get; set; }
    public decimal EstimatedCaloriesBurned { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ExerciseSessionDto> ExerciseSessions { get; set; } = new();
}