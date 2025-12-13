namespace HealthSync.Application.Interfaces;

/// <summary>
/// Interface for admin dashboard service
/// Provides statistics and analytics for admin dashboard
/// </summary>
public interface IDashboardAdminService
{
    /// <summary>
    /// Get main dashboard statistics (3 key metrics)
    /// - Total active users
    /// - New users this month
    /// - Total workouts logged today
    /// </summary>
    Task<(bool Success, object? Data, string Message)> GetDashboardStatsAsync();

    /// <summary>
    /// Get detailed statistics with additional metrics
    /// </summary>
    Task<(bool Success, object? Data, string Message)> GetDetailedStatsAsync();

    /// <summary>
    /// Get top content (top 5 exercises and top 5 forum categories)
    /// </summary>
    Task<(bool Success, object? Data, string Message)> GetTopContentAsync();

    /// <summary>
    /// Get users ordered by contribution points (descending)
    /// </summary>
    Task<(bool Success, object? Data, string Message)> GetUsersByContributionPointsAsync();
}
