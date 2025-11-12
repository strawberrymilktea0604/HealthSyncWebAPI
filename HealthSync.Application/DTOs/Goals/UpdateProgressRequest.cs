using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Goals;

public class UpdateProgressRequest
{
    public decimal? RecordedValue { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? HipCm { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
}