using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Goals;

public class CreateProgressRecordRequest
{
    [Required(ErrorMessage = "GoalId is required")]
    public int GoalId { get; set; }

    [Required(ErrorMessage = "RecordDate is required")]
    public DateTime RecordDate { get; set; }

    [Required(ErrorMessage = "RecordedValue is required")]
    [Range(0.01, 1000, ErrorMessage = "RecordedValue must be between 0.01 and 1000")]
    public decimal RecordedValue { get; set; }

    [Range(0.01, 500, ErrorMessage = "WeightKg must be between 0.01 and 500")]
    public decimal? WeightKg { get; set; }

    [Range(0.01, 300, ErrorMessage = "WaistCm must be between 0.01 and 300")]
    public decimal? WaistCm { get; set; }

    [Range(0.01, 300, ErrorMessage = "ChestCm must be between 0.01 and 300")]
    public decimal? ChestCm { get; set; }

    [Range(0.01, 300, ErrorMessage = "HipCm must be between 0.01 and 300")]
    public decimal? HipCm { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}
