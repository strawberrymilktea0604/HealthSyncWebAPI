namespace HealthSync.Application.Interfaces;

/// <summary>
/// Service interface for calculating leaderboard points
/// </summary>
public interface ILeaderboardCalculationService
{
    /// <summary>
    /// Calculate contribution points for a specific user in the current month
    /// </summary>
    Task<int> CalculateUserPointsAsync(int userId);

    /// <summary>
    /// Update leaderboard points for a specific user
    /// </summary>
    Task UpdateUserPointsAsync(int userId);

    /// <summary>
    /// Update leaderboard points for all active users
    /// </summary>
    Task UpdateAllUsersPointsAsync();
}