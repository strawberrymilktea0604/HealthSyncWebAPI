using HealthSync.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthSync.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for updating UserProfile contribution points from Leaderboard
/// Runs periodically to sync TotalPoints from Leaderboard to UserProfile.ContributionPoints
/// </summary>
public class LeaderboardUpdateJob : ILeaderboardUpdateJob
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<LeaderboardUpdateJob> _logger;

    public LeaderboardUpdateJob(
        ILeaderboardRepository leaderboardRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<LeaderboardUpdateJob> logger)
    {
        _leaderboardRepository = leaderboardRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    /// <summary>
    /// Update all users' contribution points from Leaderboard
    /// </summary>
    public async Task UpdateUserContributionPointsAsync()
    {
        try
        {
            _logger.LogInformation("[LeaderboardUpdateJob] Starting to update all users' contribution points");

            // Get all leaderboard entries
            var leaderboardEntries = await _leaderboardRepository.GetAllAsync();

            if (!leaderboardEntries.Any())
            {
                _logger.LogInformation("[LeaderboardUpdateJob] No leaderboard entries found");
                return;
            }

            int updatedCount = 0;
            int errorCount = 0;

            // Update each user's profile with points from leaderboard
            foreach (var entry in leaderboardEntries)
            {
                try
                {
                    var userProfile = await _userProfileRepository.GetByUserIdAsync(entry.UserId);
                    
                    if (userProfile is null)
                    {
                        _logger.LogWarning($"[LeaderboardUpdateJob] UserProfile not found for UserId: {entry.UserId}");
                        errorCount++;
                        continue;
                    }

                    // Update contribution points if different
                    if (userProfile.ContributionPoints != entry.TotalPoints)
                    {
                        _logger.LogInformation(
                            $"[LeaderboardUpdateJob] Updating UserId {entry.UserId}: " +
                            $"{userProfile.ContributionPoints} → {entry.TotalPoints} points");

                        userProfile.ContributionPoints = entry.TotalPoints;
                        userProfile.UpdatedAt = DateTime.UtcNow;

                        await _userProfileRepository.UpdateAsync(userProfile);
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"[LeaderboardUpdateJob] Error updating UserId {entry.UserId}: {ex.Message}");
                    errorCount++;
                }
            }

            // Save all changes
            await _userProfileRepository.SaveChangesAsync();

            _logger.LogInformation(
                $"[LeaderboardUpdateJob] Completed. Updated: {updatedCount}, Errors: {errorCount}, " +
                $"Total entries: {leaderboardEntries.Count()}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[LeaderboardUpdateJob] Fatal error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Update a specific user's contribution points from Leaderboard
    /// </summary>
    public async Task UpdateUserContributionPointsAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"[LeaderboardUpdateJob] Starting to update UserId {userId}'s contribution points");

            // Get leaderboard entry for user
            var leaderboardEntry = await _leaderboardRepository.GetByUserIdAsync(userId);

            if (leaderboardEntry is null)
            {
                _logger.LogWarning($"[LeaderboardUpdateJob] Leaderboard entry not found for UserId: {userId}");
                return;
            }

            // Get user profile
            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);

            if (userProfile is null)
            {
                _logger.LogWarning($"[LeaderboardUpdateJob] UserProfile not found for UserId: {userId}");
                return;
            }

            // Update if different
            if (userProfile.ContributionPoints != leaderboardEntry.TotalPoints)
            {
                _logger.LogInformation(
                    $"[LeaderboardUpdateJob] Updating UserId {userId}: " +
                    $"{userProfile.ContributionPoints} → {leaderboardEntry.TotalPoints} points");

                userProfile.ContributionPoints = leaderboardEntry.TotalPoints;
                userProfile.UpdatedAt = DateTime.UtcNow;

                await _userProfileRepository.UpdateAsync(userProfile);
                await _userProfileRepository.SaveChangesAsync();

                _logger.LogInformation($"[LeaderboardUpdateJob] Successfully updated UserId {userId}");
            }
            else
            {
                _logger.LogInformation(
                    $"[LeaderboardUpdateJob] UserId {userId} already has correct points ({userProfile.ContributionPoints})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[LeaderboardUpdateJob] Error updating UserId {userId}: {ex.Message}");
            throw;
        }
    }
}
