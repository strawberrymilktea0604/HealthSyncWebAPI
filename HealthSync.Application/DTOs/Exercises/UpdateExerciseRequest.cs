namespace HealthSync.Application.DTOs.Exercises;

public class UpdateExerciseRequest 
{
    public string Name { get; set; } = null!;
    public string? MuscleGroup { get; set; }
    public string? Difficulty { get; set; }
    public string? Equipment { get; set; }
    public string? Description { get; set; }
}