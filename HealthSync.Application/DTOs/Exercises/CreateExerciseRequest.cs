namespace HealthSync.Application.DTOs.Exercises;

public class CreateExerciseRequest
{
    public string Name { get; set; } = null!;
    public string MuscleGroup { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public string? Equipment { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal? CaloriesPerMinute { get; set; }
}