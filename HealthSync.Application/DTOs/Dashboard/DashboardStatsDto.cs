namespace HealthSync.Application.DTOs.Dashboard;

/// <summary>
/// DTO for main dashboard statistics
/// Contains the 3 key metrics for admin dashboard
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Total number of active users (is_active = true)
    /// </summary>
    public int TotalActiveUsers { get; set; }

    /// <summary>
    /// Number of new users created in the current month
    /// </summary>
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// Number of workout logs created today
    /// </summary>
    public int WorkoutLogsToday { get; set; }

    /// <summary>
    /// Timestamp when stats were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for detailed dashboard statistics
/// Extends basic stats with additional metrics
/// </summary>
public class DetailedDashboardStatsDto
{
    /// <summary>
    /// Total number of active users
    /// </summary>
    public int TotalActiveUsers { get; set; }

    /// <summary>
    /// Number of new users this month
    /// </summary>
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// Number of workout logs created today
    /// </summary>
    public int WorkoutLogsToday { get; set; }

    /// <summary>
    /// Total nutrition logs created today
    /// </summary>
    public int NutritionLogsToday { get; set; }

    /// <summary>
    /// Total forum posts created this month
    /// </summary>
    public int ForumPostsThisMonth { get; set; }

    /// <summary>
    /// Total forum replies created this month
    /// </summary>
    public int ForumRepliesThisMonth { get; set; }

    /// <summary>
    /// Number of open challenges
    /// </summary>
    public int OpenChallenges { get; set; }

    /// <summary>
    /// Number of pending challenge submissions waiting for approval
    /// </summary>
    public int PendingChallengeSubmissions { get; set; }

    /// <summary>
    /// Timestamp when stats were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
