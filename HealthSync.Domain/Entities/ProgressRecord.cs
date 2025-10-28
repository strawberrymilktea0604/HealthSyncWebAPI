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
    public decimal RecordValue { get; set; }
    public decimal? Weight { get; set; } // kg
    public decimal? WaistCm { get; set; }
}