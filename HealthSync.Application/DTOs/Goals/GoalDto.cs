using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Goals;

public class CreateGoalRequest
{
    [Required]
    public string GoalType { get; set; } = null!;
    
    [Required]
    [Range(0.1, double.MaxValue, ErrorMessage = "Target value must be greater than zero")]
    public decimal TargetValue { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
}

public class UpdateGoalRequest
{
    public decimal? TargetValue { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class GoalDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GoalType { get; set; } = null!;
    public decimal TargetValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    
    // Computed progress (could be filled by service layer)
    public decimal? CurrentValue { get; set; }
    public decimal? ProgressPercentage { get; set; }
    public int DaysRemaining => (EndDate - DateTime.UtcNow).Days;
    public bool IsCompleted => Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
}