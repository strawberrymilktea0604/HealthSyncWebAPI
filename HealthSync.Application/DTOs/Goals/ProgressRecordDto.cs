using System;

namespace HealthSync.Application.DTOs.Goals;

public class ProgressRecordDto
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public DateTime RecordDate { get; set; }
    public decimal RecordedValue { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? HipCm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}