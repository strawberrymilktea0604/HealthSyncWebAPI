using System;

namespace HealthSync.Application.DTOs.Goals;

public class GoalResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GoalType { get; set; } = null!;
    public double TargetValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}