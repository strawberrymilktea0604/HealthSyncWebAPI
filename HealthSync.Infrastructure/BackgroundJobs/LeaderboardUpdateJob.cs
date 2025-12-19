using HealthSync.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthSync.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for calculating and updating user contribution points
/// Step 1: Calculate points using PointCalculationService
/// Step 2: Update Leaderboard.TotalPoints
/// Step 3: Sync to UserProfile.ContributionPoints
/// </summary>
public class LeaderboardUpdateJob : ILeaderboardUpdateJob
{
    private readonly IPointCalculationService _pointCalculationService;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<LeaderboardUpdateJob> _logger;

    public LeaderboardUpdateJob(
        IPointCalculationService pointCalculationService,
        ILeaderboardRepository leaderboardRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<LeaderboardUpdateJob> logger)
    {
        _pointCalculationService = pointCalculationService;
        _leaderboardRepository = leaderboardRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    /// <summary>
    /// Update all users' contribution points
    /// 1. Calculate points for all users
    /// 2. Update Leaderboard entries
    /// 3. Sync to UserProfile
    /// </summary>
    public async Task UpdateUserContributionPointsAsync()
    {
        try
        {
            _logger.LogInformation("[LeaderboardUpdateJob] Starting complete point calculation and update process");

            // Step 1: Calculate and update points in Leaderboard for all users
            var updatedCount = await _pointCalculationService.CalculateAndUpdateAllUserPointsAsync();
            _logger.LogInformation("[LeaderboardUpdateJob] Point calculation completed for {Count} users", updatedCount);

            // Step 2: Sync Leaderboard points to UserProfile
            await SyncLeaderboardToUserProfilesAsync();

            _logger.LogInformation("[LeaderboardUpdateJob] Complete update process finished successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LeaderboardUpdateJob] Fatal error in update process");
            throw new InvalidOperationException("Fatal error in leaderboard update process", ex);
        }
    }

    /// <summary>
    /// Update a specific user's contribution points
    /// 1. Calculate points for the user
    /// 2. Update Leaderboard entry
    /// 3. Sync to UserProfile
    /// </summary>
    public async Task UpdateUserContributionPointsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("[LeaderboardUpdateJob] Starting update process for UserId: {UserId}", userId);

            // Step 1: Calculate points for this user
            var calculatedPoints = await _pointCalculationService.CalculateUserPointsAsync(userId);

            // Step 2: Update or create Leaderboard entry
            var leaderboardEntry = await _leaderboardRepository.GetByUserIdAsync(userId);
            
            if (leaderboardEntry == null)
            {
                leaderboardEntry = new HealthSync.Domain.Entities.Leaderboard
                {
                    UserId = userId,
                    TotalPoints = calculatedPoints,
                    UpdatedAt = DateTime.UtcNow
                };
                await _leaderboardRepository.AddAsync(leaderboardEntry);
                _logger.LogInformation("[LeaderboardUpdateJob] Created leaderboard entry for UserId {UserId} with {Points} points", 
                    userId, calculatedPoints);
            }
            else if (leaderboardEntry.TotalPoints != calculatedPoints)
            {
                leaderboardEntry.TotalPoints = calculatedPoints;
                leaderboardEntry.UpdatedAt = DateTime.UtcNow;
                await _leaderboardRepository.UpdateAsync(leaderboardEntry);
                _logger.LogInformation("[LeaderboardUpdateJob] Updated leaderboard for UserId {UserId} to {Points} points", 
                    userId, calculatedPoints);
            }

            await _leaderboardRepository.SaveChangesAsync();

            // Step 3: Sync to UserProfile
            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            
            if (userProfile != null && userProfile.ContributionPoints != calculatedPoints)
            {
                userProfile.ContributionPoints = calculatedPoints;
                userProfile.UpdatedAt = DateTime.UtcNow;
                await _userProfileRepository.UpdateAsync(userProfile);
                await _userProfileRepository.SaveChangesAsync();
                
                _logger.LogInformation("[LeaderboardUpdateJob] Synced points to UserProfile for UserId {UserId}", userId);
            }

            _logger.LogInformation("[LeaderboardUpdateJob] Successfully completed update for UserId {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LeaderboardUpdateJob] Error updating UserId {UserId}", userId);
            throw new InvalidOperationException($"Error updating contribution points for UserId {userId}", ex);
        }
    }

    /// <summary>
    /// Private helper: Sync all Leaderboard entries to UserProfile
    /// </summary>
    private async Task SyncLeaderboardToUserProfilesAsync()
    {
        try
        {
            _logger.LogInformation("[LeaderboardUpdateJob] Starting sync from Leaderboard to UserProfile");

            var leaderboardEntries = await _leaderboardRepository.GetAllAsync();
            int syncedCount = 0;
            int errorCount = 0;

            foreach (var entry in leaderboardEntries)
            {
                try
                {
                    var userProfile = await _userProfileRepository.GetByUserIdAsync(entry.UserId);
                    
                    if (userProfile == null)
                    {
                        _logger.LogWarning("[LeaderboardUpdateJob] UserProfile not found for UserId: {UserId}", entry.UserId);
                        errorCount++;
                        continue;
                    }

                    if (userProfile.ContributionPoints != entry.TotalPoints)
                    {
                        userProfile.ContributionPoints = entry.TotalPoints;
                        userProfile.UpdatedAt = DateTime.UtcNow;
                        await _userProfileRepository.UpdateAsync(userProfile);
                        syncedCount++;
                        
                        _logger.LogDebug("[LeaderboardUpdateJob] Synced UserId {UserId}: {Points} points", 
                            entry.UserId, entry.TotalPoints);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LeaderboardUpdateJob] Error syncing UserId: {UserId}", entry.UserId);
                    errorCount++;
                }
            }

            await _userProfileRepository.SaveChangesAsync();

            _logger.LogInformation(
                "[LeaderboardUpdateJob] Sync completed. Synced: {SyncedCount}, Errors: {ErrorCount}, Total: {TotalCount}",
                syncedCount, errorCount, leaderboardEntries.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LeaderboardUpdateJob] Fatal error in sync process");
            throw new InvalidOperationException("Fatal error in syncing leaderboard to user profiles", ex);
        }
    }
}
