using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HealthSync.Application.Services;

/// <summary>
/// Service for calculating user contribution points based on activities
/// Point Calculation Formula:
/// - WorkoutLog: 5 points each
/// - Forum Post: 2 points each
/// - Forum Reply: 1 point each
/// - Completed Challenge: 10 points each
/// </summary>
public class PointCalculationService : IPointCalculationService
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly IForumPostRepository _forumPostRepository;
    private readonly IForumReplyRepository _forumReplyRepository;
    private readonly IChallengeParticipationRepository _challengeParticipationRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PointCalculationService> _logger;

    // Point values constants
    private const int WORKOUT_LOG_POINTS = 5;
    private const int FORUM_POST_POINTS = 2;
    private const int FORUM_REPLY_POINTS = 1;
    private const int COMPLETED_CHALLENGE_POINTS = 10;

    public PointCalculationService(
        IWorkoutLogRepository workoutLogRepository,
        IForumPostRepository forumPostRepository,
        IForumReplyRepository forumReplyRepository,
        IChallengeParticipationRepository challengeParticipationRepository,
        ILeaderboardRepository leaderboardRepository,
        IUserRepository userRepository,
        ILogger<PointCalculationService> logger)
    {
        _workoutLogRepository = workoutLogRepository;
        _forumPostRepository = forumPostRepository;
        _forumReplyRepository = forumReplyRepository;
        _challengeParticipationRepository = challengeParticipationRepository;
        _leaderboardRepository = leaderboardRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Calculate total contribution points for a specific user
    /// Formula: (WorkoutLogs * 5) + (Posts * 2) + (Replies * 1) + (CompletedChallenges * 10)
    /// </summary>
    public async Task<int> CalculateUserPointsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("[PointCalculationService] Calculating points for UserId: {UserId}", userId);

            // Count workout logs for user
            var workoutLogsCount = await CountUserWorkoutLogsAsync(userId);
            _logger.LogDebug("[PointCalculationService] UserId {UserId}: {Count} workout logs", userId, workoutLogsCount);

            // Count forum posts for user
            var forumPostsCount = await CountUserForumPostsAsync(userId);
            _logger.LogDebug("[PointCalculationService] UserId {UserId}: {Count} forum posts", userId, forumPostsCount);

            // Count forum replies for user
            var forumRepliesCount = await CountUserForumRepliesAsync(userId);
            _logger.LogDebug("[PointCalculationService] UserId {UserId}: {Count} forum replies", userId, forumRepliesCount);

            // Count completed challenges for user
            var completedChallengesCount = await CountUserCompletedChallengesAsync(userId);
            _logger.LogDebug("[PointCalculationService] UserId {UserId}: {Count} completed challenges", userId, completedChallengesCount);

            // Calculate total points
            var totalPoints = (workoutLogsCount * WORKOUT_LOG_POINTS) +
                            (forumPostsCount * FORUM_POST_POINTS) +
                            (forumRepliesCount * FORUM_REPLY_POINTS) +
                            (completedChallengesCount * COMPLETED_CHALLENGE_POINTS);

            _logger.LogInformation(
                "[PointCalculationService] UserId {UserId} total points: {TotalPoints} " +
                "(Workouts: {Workouts}*{WorkoutPoints} + Posts: {Posts}*{PostPoints} + " +
                "Replies: {Replies}*{ReplyPoints} + Challenges: {Challenges}*{ChallengePoints})",
                userId, totalPoints,
                workoutLogsCount, WORKOUT_LOG_POINTS,
                forumPostsCount, FORUM_POST_POINTS,
                forumRepliesCount, FORUM_REPLY_POINTS,
                completedChallengesCount, COMPLETED_CHALLENGE_POINTS);

            return totalPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PointCalculationService] Error calculating points for UserId: {UserId}", userId);
            throw new InvalidOperationException($"Error calculating points for UserId {userId}", ex);
        }
    }

    /// <summary>
    /// Calculate and update points for all users in the leaderboard
    /// </summary>
    public async Task<int> CalculateAndUpdateAllUserPointsAsync()
    {
        try
        {
            _logger.LogInformation("[PointCalculationService] Starting to calculate and update points for all users");

            // Get all users
            var users = await _userRepository.GetAllAsync();
            var updatedCount = 0;
            var errorCount = 0;

            foreach (var user in users)
            {
                try
                {
                    // Calculate points for this user
                    var calculatedPoints = await CalculateUserPointsAsync(user.UserId);

                    // Get or create leaderboard entry
                    var leaderboardEntry = await _leaderboardRepository.GetByUserIdAsync(user.UserId);

                    if (leaderboardEntry == null)
                    {
                        // Create new leaderboard entry
                        leaderboardEntry = new Leaderboard
                        {
                            UserId = user.UserId,
                            TotalPoints = calculatedPoints,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _leaderboardRepository.AddAsync(leaderboardEntry);
                        _logger.LogInformation(
                            "[PointCalculationService] Created new leaderboard entry for UserId {UserId} with {Points} points",
                            user.UserId, calculatedPoints);
                    }
                    else if (leaderboardEntry.TotalPoints != calculatedPoints)
                    {
                        // Update existing entry
                        var oldPoints = leaderboardEntry.TotalPoints;
                        leaderboardEntry.TotalPoints = calculatedPoints;
                        leaderboardEntry.UpdatedAt = DateTime.UtcNow;
                        await _leaderboardRepository.UpdateAsync(leaderboardEntry);
                        _logger.LogInformation(
                            "[PointCalculationService] Updated UserId {UserId}: {OldPoints} → {NewPoints} points",
                            user.UserId, oldPoints, calculatedPoints);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "[PointCalculationService] No change for UserId {UserId}: {Points} points",
                            user.UserId, calculatedPoints);
                    }

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PointCalculationService] Error updating UserId: {UserId}", user.UserId);
                    errorCount++;
                }
            }

            await _leaderboardRepository.SaveChangesAsync();

            _logger.LogInformation(
                "[PointCalculationService] Completed. Updated: {UpdatedCount}, Errors: {ErrorCount}, Total users: {TotalCount}",
                updatedCount, errorCount, users.Count());

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PointCalculationService] Fatal error in CalculateAndUpdateAllUserPointsAsync");
            throw new InvalidOperationException("Fatal error in calculating and updating all user points", ex);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Count total workout logs for a user
    /// </summary>
    private async Task<int> CountUserWorkoutLogsAsync(int userId)
    {
        // Use pagination to get count efficiently
        var result = await _workoutLogRepository.GetByUserIdAsync(userId, 1, 1);
        return result.TotalItems;
    }

    /// <summary>
    /// Count total forum posts created by a user
    /// </summary>
    private async Task<int> CountUserForumPostsAsync(int userId)
    {
        var allPosts = await _forumPostRepository.GetAllPostsAsync();
        return allPosts.Count(p => p.UserId == userId);
    }

    /// <summary>
    /// Count total forum replies created by a user
    /// </summary>
    private async Task<int> CountUserForumRepliesAsync(int userId)
    {
        var allReplies = await _forumReplyRepository.GetAllAsync();
        return allReplies.Count(r => r.UserId == userId);
    }

    /// <summary>
    /// Count total completed challenges for a user
    /// </summary>
    private async Task<int> CountUserCompletedChallengesAsync(int userId)
    {
        var allParticipations = await _challengeParticipationRepository.GetAllAsync();
        return allParticipations.Count(cp => cp.UserId == userId && cp.Status == ParticipationStatus.Completed);
    }

    #endregion
}
