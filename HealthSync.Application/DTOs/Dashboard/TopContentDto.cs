namespace HealthSync.Application.DTOs.Dashboard;

/// <summary>
/// DTO for top exercises
/// </summary>
public class TopExerciseDto
{
    public int ExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

/// <summary>
/// DTO for top forum categories
/// </summary>
public class TopForumCategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public int ReplyCount { get; set; }
    public int TotalActivity { get; set; }
}

/// <summary>
/// DTO for top content (exercises and forum categories)
/// </summary>
public class TopContentDto
{
    public List<TopExerciseDto> TopExercises { get; set; } = new();
    public List<TopForumCategoryDto> TopForumCategories { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
