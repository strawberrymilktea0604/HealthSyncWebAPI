namespace HealthSync.Application.Interfaces;

/// <summary>
/// Background job interface for updating user contribution points to UserProfile
/// </summary>
public interface ILeaderboardUpdateJob
{
    /// <summary>
    /// Update UserProfile contribution points from Leaderboard
    /// This job synchronizes TotalPoints from Leaderboard table to UserProfile.ContributionPoints
    /// </summary>
    Task UpdateUserContributionPointsAsync();

    /// <summary>
    /// Update contribution points for a specific user
    /// </summary>
    Task UpdateUserContributionPointsAsync(int userId);
}
