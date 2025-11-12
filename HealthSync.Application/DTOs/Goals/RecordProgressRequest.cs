using System;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Goals;

public class RecordProgressRequest
{
    [Required(ErrorMessage = "Goal ID is required")]
    public int GoalId { get; set; }

    [Required(ErrorMessage = "Record date is required")]
    public DateTime RecordDate { get; set; }

    [Required(ErrorMessage = "Recorded value is required")]
    [Range(0.1, double.MaxValue, ErrorMessage = "Recorded value must be greater than 0")]
    public decimal RecordedValue { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Weight must be greater than 0")]
    public decimal? WeightKg { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Waist measurement must be greater than 0")]
    public decimal? WaistCm { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Chest measurement must be greater than 0")]
    public decimal? ChestCm { get; set; }

    [Range(0.1, double.MaxValue, ErrorMessage = "Hip measurement must be greater than 0")]
    public decimal? HipCm { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}