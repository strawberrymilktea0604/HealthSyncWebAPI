using System;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Goals;

public class RecordProgressRequest
{
    [Required(ErrorMessage = "Record date is required")]
    public DateTime RecordDate { get; set; }

    [Required(ErrorMessage = "Recorded value is required")]
    [Range(0.1, double.MaxValue, ErrorMessage = "Recorded value must be greater than 0")]
    public decimal RecordedValue { get; set; }

    public decimal? WeightKg { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? HipCm { get; set; }

    public string? Notes { get; set; }
}