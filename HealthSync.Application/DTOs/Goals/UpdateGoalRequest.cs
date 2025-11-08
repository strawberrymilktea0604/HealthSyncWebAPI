using System;

namespace HealthSync.Application.DTOs.Goals;

public class UpdateGoalRequest
{
    public string GoalType { get; set; } = null!;
    public double TargetValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
}