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
    public string GoalType { get; set; } = null!; // WeightLoss, WeightGain, MuscleGain, Endurance
    public decimal TargetValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!; // Active, Completed, Paused, Cancelled

    // Navigation properties
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
}