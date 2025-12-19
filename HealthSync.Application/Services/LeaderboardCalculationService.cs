using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

/// <summary>
/// Service for calculating and updating user leaderboard points
/// </summary>
public class LeaderboardCalculationService : ILeaderboardCalculationService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly IForumPostRepository _forumPostRepository;
    private readonly IForumReplyRepository _forumReplyRepository;
    private readonly IChallengeParticipationRepository _challengeParticipationRepository;
    private readonly IUserRepository _userRepository;

    public LeaderboardCalculationService(
        ILeaderboardRepository leaderboardRepository,
        IWorkoutLogRepository workoutLogRepository,
        IForumPostRepository forumPostRepository,
        IForumReplyRepository forumReplyRepository,
        IChallengeParticipationRepository challengeParticipationRepository,
        IUserRepository userRepository)
    {
        _leaderboardRepository = leaderboardRepository;
        _workoutLogRepository = workoutLogRepository;
        _forumPostRepository = forumPostRepository;
        _forumReplyRepository = forumReplyRepository;
        _challengeParticipationRepository = challengeParticipationRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Calculate contribution points for a specific user in the current month
    /// Formula: (workout_logs * 5) + (posts * 2) + (replies * 1) + (completed_challenges * 10)
    /// </summary>
    public async Task<int> CalculateUserPointsAsync(int userId)
    {
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        // Count workout logs in current month
        var workoutLogsCount = await _workoutLogRepository.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth);

        // Count posts in current month
        var postsCount = await _forumPostRepository.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth);

        // Count replies in current month
        var repliesCount = await _forumReplyRepository.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth);

        // Count completed challenges in current month
        var completedChallengesCount = await _challengeParticipationRepository.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth);

        // Calculate total points using the formula
        var totalPoints = (workoutLogsCount * 5) +
                         (postsCount * 2) +
                         (repliesCount * 1) +
                         (completedChallengesCount * 10);

        return totalPoints;
    }

    /// <summary>
    /// Update leaderboard points for a specific user
    /// </summary>
    public async Task UpdateUserPointsAsync(int userId)
    {
        var totalPoints = await CalculateUserPointsAsync(userId);

        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(userId);
        if (leaderboard == null)
        {
            // Create new leaderboard entry if not exists
            leaderboard = new Leaderboard
            {
                UserId = userId,
                TotalPoints = totalPoints,
                UpdatedAt = DateTime.UtcNow
            };
            await _leaderboardRepository.AddAsync(leaderboard);
        }
        else
        {
            // Update existing
            leaderboard.TotalPoints = totalPoints;
            leaderboard.UpdatedAt = DateTime.UtcNow;
            await _leaderboardRepository.UpdateAsync(leaderboard);
        }

        await _leaderboardRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Update leaderboard points for all active users
    /// This method should be called by a background job periodically
    /// </summary>
    public async Task UpdateAllUsersPointsAsync()
    {
        var activeUsers = await _userRepository.GetActiveUsersAsync();

        foreach (var user in activeUsers)
        {
            await UpdateUserPointsAsync(user.UserId);
        }

        // After updating all, we could update rank positions if needed
        await UpdateRankPositionsAsync();
    }

    /// <summary>
    /// Update rank positions for top users (optional)
    /// </summary>
    private static Task UpdateRankPositionsAsync()
    {
        // This could be implemented to set rank_position in Leaderboard table
        // For now, it's optional as per requirements
        return Task.CompletedTask;
    }
}