using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class WorkoutLog
{
    public int WorkoutLogId { get; set; }

    // Owner
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Workout details
    public DateTime WorkoutDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<ExerciseSession> ExerciseSessions { get; set; } = new List<ExerciseSession>();
}