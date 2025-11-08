namespace HealthSync.Application.DTOs.Exercises;

public class ExerciseResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? MuscleGroup { get; set; }
    public string? Difficulty { get; set; }
    public string? Equipment { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal? CaloriesPerMinute { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}