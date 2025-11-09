using System;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Goals;

public class GoalResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public GoalType GoalType { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public GoalStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}