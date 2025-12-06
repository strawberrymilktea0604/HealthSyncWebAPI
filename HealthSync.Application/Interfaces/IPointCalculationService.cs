namespace HealthSync.Application.Interfaces;

/// <summary>
/// Interface for calculating user contribution points based on activities
/// Points calculation formula:
/// - WorkoutLog: 5 points each
/// - Forum Post: 2 points each
/// - Forum Reply: 1 point each
/// - Completed Challenge: 10 points each
/// </summary>
public interface IPointCalculationService
{
    /// <summary>
    /// Calculate total contribution points for a specific user
    /// </summary>
    /// <param name="userId">User ID to calculate points for</param>
    /// <returns>Total calculated points</returns>
    Task<int> CalculateUserPointsAsync(int userId);

    /// <summary>
    /// Calculate and update points for all users
    /// </summary>
    /// <returns>Number of users updated</returns>
    Task<int> CalculateAndUpdateAllUserPointsAsync();
}
