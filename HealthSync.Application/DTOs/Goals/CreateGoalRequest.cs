using System;
using System.ComponentModel.DataAnnotations;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Goals;

public class CreateGoalRequest
{
    [Required(ErrorMessage = "Goal type is required")]
    public GoalType GoalType { get; set; }

    [Required(ErrorMessage = "Target value is required")]
    [Range(0.1, double.MaxValue, ErrorMessage = "Target value must be greater than 0")]
    public decimal TargetValue { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [RegularExpression("kg|cm|%", ErrorMessage = "Unit must be one of: kg, cm, %")]
    public string Unit { get; set; } = null!;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}