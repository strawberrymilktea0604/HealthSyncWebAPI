using System;

namespace HealthSync.Domain.Entities;

public class ProgressRecord
{
    public int ProgressRecordId { get; set; }

    // Goal reference
    public int GoalId { get; set; }
    public Goal Goal { get; set; } = null!;

    // Progress data
    public DateTime RecordDate { get; set; }
    public decimal RecordedValue { get; set; }
    public decimal? WeightKg { get; set; } // kg
    public decimal? WaistCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? HipCm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}