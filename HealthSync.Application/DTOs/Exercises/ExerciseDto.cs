namespace HealthSync.Application.DTOs.Exercises;

public class ExerciseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string MuscleGroup { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public string? Equipment { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}