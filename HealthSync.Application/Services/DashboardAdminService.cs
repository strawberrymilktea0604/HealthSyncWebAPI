using HealthSync.Application.DTOs.Dashboard;
using HealthSync.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthSync.Application.Services;

/// <summary>
/// Service for providing admin dashboard statistics and analytics
/// </summary>
public class DashboardAdminService : IDashboardAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly INutritionLogRepository _nutritionLogRepository;
    private readonly IForumPostRepository _forumPostRepository;
    private readonly IForumReplyRepository _forumReplyRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeParticipationRepository _participationRepository;
    private readonly ILogger<DashboardAdminService> _logger;

    public DashboardAdminService(
        IUserRepository userRepository,
        IWorkoutLogRepository workoutLogRepository,
        INutritionLogRepository nutritionLogRepository,
        IForumPostRepository forumPostRepository,
        IForumReplyRepository forumReplyRepository,
        IChallengeRepository challengeRepository,
        IChallengeParticipationRepository participationRepository,
        ILogger<DashboardAdminService> logger)
    {
        _userRepository = userRepository;
        _workoutLogRepository = workoutLogRepository;
        _nutritionLogRepository = nutritionLogRepository;
        _forumPostRepository = forumPostRepository;
        _forumReplyRepository = forumReplyRepository;
        _challengeRepository = challengeRepository;
        _participationRepository = participationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get main dashboard statistics (3 key metrics)
    /// </summary>
    public async Task<(bool Success, object? Data, string Message)> GetDashboardStatsAsync()
    {
        try
        {
            _logger.LogInformation("[DashboardAdminService] Calculating dashboard statistics");

            // Metric 1: Total active users
            var allUsers = await _userRepository.GetAllAsync();
            var totalActiveUsers = allUsers.Count(u => u.IsActive);

            _logger.LogInformation($"[DashboardAdminService] Total active users: {totalActiveUsers}");

            // Metric 2: New users this month
            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= firstDayOfMonth);

            _logger.LogInformation($"[DashboardAdminService] New users this month: {newUsersThisMonth}");

            // Metric 3: Workouts logged today
            // Note: This requires querying from repository
            // For now, we'll estimate by checking if GetByUserIdAsync works per user
            // In production, add a dedicated repository method for this
            var workoutLogsToday = 0;
            foreach (var user in allUsers.Where(u => u.IsActive))
            {
                try
                {
                    var result = await _workoutLogRepository.GetByUserIdAsync(user.UserId, 1, int.MaxValue, today, today);
                    workoutLogsToday += result.Items.Count();
                }
                catch
                {
                    // Skip if error for this user
                }
            }

            _logger.LogInformation($"[DashboardAdminService] Workout logs today: {workoutLogsToday}");

            var stats = new DashboardStatsDto
            {
                TotalActiveUsers = totalActiveUsers,
                NewUsersThisMonth = newUsersThisMonth,
                WorkoutLogsToday = workoutLogsToday,
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[DashboardAdminService] Dashboard statistics calculated successfully");

            return (true, stats, "Dashboard statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DashboardAdminService] Error calculating dashboard stats: {ex.Message}");
            return (false, null, $"Error calculating dashboard statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Get detailed statistics with additional metrics
    /// </summary>
    public async Task<(bool Success, object? Data, string Message)> GetDetailedStatsAsync()
    {
        try
        {
            _logger.LogInformation("[DashboardAdminService] Calculating detailed statistics");

            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // Get all users
            var allUsers = await _userRepository.GetAllAsync();
            var totalActiveUsers = allUsers.Count(u => u.IsActive);

            // New users this month
            var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= firstDayOfMonth);

            // Workouts today
            var workoutLogsToday = 0;
            foreach (var user in allUsers.Where(u => u.IsActive))
            {
                try
                {
                    var result = await _workoutLogRepository.GetByUserIdAsync(user.UserId, 1, int.MaxValue, today, today);
                    workoutLogsToday += result.Items.Count();
                }
                catch
                {
                    // Skip if error
                }
            }

            // Nutrition logs today
            var nutritionLogsToday = 0;
            try
            {
                var allNutritionLogs = await _nutritionLogRepository.GetAllAsync();
                nutritionLogsToday = allNutritionLogs.Count(nl => nl.LogDate.Date == today.Date);
            }
            catch { }

            // Forum posts this month
            var forumPostsThisMonth = 0;
            try
            {
                var allPosts = await _forumPostRepository.GetAllPostsAsync();
                forumPostsThisMonth = allPosts.Count(p => p.CreatedAt >= firstDayOfMonth);
            }
            catch { }

            // Forum replies this month
            var forumRepliesThisMonth = 0;
            try
            {
                var allReplies = await _forumReplyRepository.GetAllAsync();
                forumRepliesThisMonth = allReplies.Count(r => r.CreatedAt >= firstDayOfMonth);
            }
            catch { }

            // Open challenges
            var openChallenges = 0;
            try
            {
                var (challenges, _) = await _challengeRepository.GetAllAsync(1, int.MaxValue);
                openChallenges = challenges.Count(c => c.Status.ToString() == "Open");
            }
            catch { }

            // Pending challenge submissions
            var pendingSubmissions = 0;
            try
            {
                var allParticipations = await _participationRepository.GetAllAsync();
                pendingSubmissions = allParticipations.Count(p => p.Status.ToString() == "PendingApproval");
            }
            catch { }

            var stats = new DetailedDashboardStatsDto
            {
                TotalActiveUsers = totalActiveUsers,
                NewUsersThisMonth = newUsersThisMonth,
                WorkoutLogsToday = workoutLogsToday,
                NutritionLogsToday = nutritionLogsToday,
                ForumPostsThisMonth = forumPostsThisMonth,
                ForumRepliesThisMonth = forumRepliesThisMonth,
                OpenChallenges = openChallenges,
                PendingChallengeSubmissions = pendingSubmissions,
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[DashboardAdminService] Detailed statistics calculated successfully");

            return (true, stats, "Detailed statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DashboardAdminService] Error calculating detailed stats: {ex.Message}");
            return (false, null, $"Error calculating statistics: {ex.Message}");
        }
    }
}
