using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class Goal
{
    public int GoalId { get; set; }

    // Owner
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public GoalType GoalType { get; set; } // WeightLoss, WeightGain, MaintainWeight, BodyMeasurement
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = null!; // kg, cm, %
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.InProgress; // InProgress, Completed, Cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
}