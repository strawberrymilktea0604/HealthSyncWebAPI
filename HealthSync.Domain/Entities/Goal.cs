using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class Goal
{
    public int GoalId { get; set; }

    // Owner
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Goal details
    public string GoalType { get; set; } = null!; // WeightLoss, WeightGain, MaintainWeight, BodyMeasurement
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = null!; // kg, cm, %
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!; // InProgress, Completed, Cancelled
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
}