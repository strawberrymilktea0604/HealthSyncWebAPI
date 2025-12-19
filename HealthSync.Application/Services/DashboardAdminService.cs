using HealthSync.Application.DTOs.Dashboard;
using HealthSync.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthSync.Application.Services;

/// <summary>
/// Dependencies container for DashboardAdminService to reduce constructor parameters
/// </summary>
public class UserDependencies
{
    public IUserRepository UserRepository { get; }
    public IUserProfileRepository UserProfileRepository { get; }

    public UserDependencies(IUserRepository userRepository, IUserProfileRepository userProfileRepository)
    {
        UserRepository = userRepository;
        UserProfileRepository = userProfileRepository;
    }
}

public class WorkoutDependencies
{
    public IWorkoutLogRepository WorkoutLogRepository { get; }
    public IExerciseRepository ExerciseRepository { get; }
    public IExerciseSessionRepository ExerciseSessionRepository { get; }

    public WorkoutDependencies(IWorkoutLogRepository workoutLogRepository, IExerciseRepository exerciseRepository, IExerciseSessionRepository exerciseSessionRepository)
    {
        WorkoutLogRepository = workoutLogRepository;
        ExerciseRepository = exerciseRepository;
        ExerciseSessionRepository = exerciseSessionRepository;
    }
}

public class ForumDependencies
{
    public IForumPostRepository ForumPostRepository { get; }
    public IForumReplyRepository ForumReplyRepository { get; }
    public IForumCategoryRepository ForumCategoryRepository { get; }

    public ForumDependencies(IForumPostRepository forumPostRepository, IForumReplyRepository forumReplyRepository, IForumCategoryRepository forumCategoryRepository)
    {
        ForumPostRepository = forumPostRepository;
        ForumReplyRepository = forumReplyRepository;
        ForumCategoryRepository = forumCategoryRepository;
    }
}

public class ChallengeDependencies
{
    public IChallengeRepository ChallengeRepository { get; }
    public IChallengeParticipationRepository ParticipationRepository { get; }

    public ChallengeDependencies(IChallengeRepository challengeRepository, IChallengeParticipationRepository participationRepository)
    {
        ChallengeRepository = challengeRepository;
        ParticipationRepository = participationRepository;
    }
}

public class DashboardDependencies
{
    public UserDependencies User { get; }
    public WorkoutDependencies Workout { get; }
    public INutritionLogRepository NutritionLogRepository { get; }
    public ForumDependencies Forum { get; }
    public ChallengeDependencies Challenge { get; }

    public DashboardDependencies(
        UserDependencies user,
        WorkoutDependencies workout,
        INutritionLogRepository nutritionLogRepository,
        ForumDependencies forum,
        ChallengeDependencies challenge)
    {
        User = user;
        Workout = workout;
        NutritionLogRepository = nutritionLogRepository;
        Forum = forum;
        Challenge = challenge;
    }
}

/// <summary>
/// Service for providing admin dashboard statistics and analytics
/// </summary>
public class DashboardAdminService : IDashboardAdminService
{
    private readonly DashboardDependencies _dependencies;
    private readonly ILogger<DashboardAdminService> _logger;

    public DashboardAdminService(
        DashboardDependencies dependencies,
        ILogger<DashboardAdminService> logger)
    {
        _dependencies = dependencies;
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
            var allUsers = await _dependencies.User.UserRepository.GetAllAsync();
            var totalActiveUsers = allUsers.Count(u => u.IsActive);

            _logger.LogInformation("Total active users: {TotalActiveUsers}", totalActiveUsers);

            // Metric 2: New users this month
            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= firstDayOfMonth);

            _logger.LogInformation("New users this month: {NewUsersThisMonth}", newUsersThisMonth);

            // Metric 3: Workouts logged today
            var workoutLogsToday = await _dependencies.Workout.WorkoutLogRepository.CountWorkoutLogsTodayAsync();

            _logger.LogInformation("Workout logs today: {WorkoutLogsToday}", workoutLogsToday);

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
            _logger.LogError(ex, "[DashboardAdminService] Error calculating dashboard stats: {Message}", ex.Message);
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
            _logger.LogInformation("Calculating detailed statistics");

            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Get all users
            var allUsers = await _dependencies.User.UserRepository.GetAllAsync();
            var totalActiveUsers = allUsers.Count(u => u.IsActive);

            // New users this month
            var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= firstDayOfMonth);

            // Workouts today
            var workoutLogsToday = await _dependencies.Workout.WorkoutLogRepository.CountWorkoutLogsTodayAsync();

            // Nutrition logs today
            var nutritionLogsToday = 0;
            try
            {
                var allNutritionLogs = await _dependencies.NutritionLogRepository.GetAllAsync();
                nutritionLogsToday = allNutritionLogs.Count(nl => nl.LogDate.Date == today.Date);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get nutrition logs count");
            }

            // Forum posts this month
            var forumPostsThisMonth = 0;
            try
            {
                var allPosts = await _dependencies.Forum.ForumPostRepository.GetAllPostsAsync();
                forumPostsThisMonth = allPosts.Count(p => p.CreatedAt >= firstDayOfMonth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get forum posts count");
            }

            // Forum replies this month
            var forumRepliesThisMonth = 0;
            try
            {
                var allReplies = await _dependencies.Forum.ForumReplyRepository.GetAllAsync();
                forumRepliesThisMonth = allReplies.Count(r => r.CreatedAt >= firstDayOfMonth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get forum replies count");
            }

            // Open challenges
            var openChallenges = 0;
            try
            {
                var (challenges, _) = await _dependencies.Challenge.ChallengeRepository.GetAllAsync(1, int.MaxValue);
                openChallenges = challenges.Count(c => c.Status.ToString() == "Open");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get open challenges count");
            }

            // Pending challenge submissions
            var pendingSubmissions = 0;
            try
            {
                var allParticipations = await _dependencies.Challenge.ParticipationRepository.GetAllAsync();
                pendingSubmissions = allParticipations.Count(p => p.Status.ToString() == "PendingApproval");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get pending submissions count");
            }

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
            _logger.LogError(ex, "[DashboardAdminService] Error calculating detailed stats: {Message}", ex.Message);
            return (false, null, $"Error calculating statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Get top content (top 5 exercises and top 5 forum categories)
    /// </summary>
    public async Task<(bool Success, object? Data, string Message)> GetTopContentAsync()
    {
        try
        {
            _logger.LogInformation("[DashboardAdminService] Calculating top content");

            // Get top 5 exercises (by usage count in ExerciseSessions)
            var topExercises = new List<TopExerciseDto>();
            try
            {
                var allExercises = await _dependencies.Workout.ExerciseRepository.GetAllAsync();
                var allSessions = await _dependencies.Workout.ExerciseSessionRepository.GetAllAsync();

                var exerciseUsage = allSessions
                    .GroupBy(s => s.ExerciseId)
                    .Select(g => new
                    {
                        ExerciseId = g.Key,
                        UsageCount = g.Count()
                    })
                    .OrderByDescending(x => x.UsageCount)
                    .Take(5)
                    .ToList();

                foreach (var usage in exerciseUsage)
                {
                    var exercise = allExercises.FirstOrDefault(e => e.ExerciseId == usage.ExerciseId);
                    if (exercise != null)
                    {
                        topExercises.Add(new TopExerciseDto
                        {
                            ExerciseId = exercise.ExerciseId,
                            Name = exercise.Name,
                            MuscleGroup = exercise.MuscleGroup.ToString(),
                            DifficultyLevel = exercise.DifficultyLevel.ToString(),
                            UsageCount = usage.UsageCount
                        });
                    }
                }

                _logger.LogInformation("[DashboardAdminService] Found {Count} top exercises", topExercises.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[DashboardAdminService] Error getting top exercises: {Message}", ex.Message);
            }

            // Get top 5 forum categories (by activity: posts + replies)
            var topForumCategories = new List<TopForumCategoryDto>();
            try
            {
                var allCategories = await _dependencies.Forum.ForumCategoryRepository.GetAllAsync();
                var allPosts = await _dependencies.Forum.ForumPostRepository.GetAllPostsAsync();
                var allReplies = await _dependencies.Forum.ForumReplyRepository.GetAllAsync();

                var categoryActivity = allCategories
                    .Select(c => new TopForumCategoryDto
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name,
                        PostCount = allPosts.Count(p => p.CategoryId == c.CategoryId),
                        ReplyCount = allReplies.Count(r => 
                            allPosts.FirstOrDefault(p => p.PostId == r.PostId)?.CategoryId == c.CategoryId),
                        TotalActivity = 0 // Will calculate below
                    })
                    .ToList();

                // Calculate total activity and sort
                foreach (var cat in categoryActivity)
                {
                    cat.TotalActivity = cat.PostCount + cat.ReplyCount;
                }

                topForumCategories = categoryActivity
                    .OrderByDescending(x => x.TotalActivity)
                    .Take(5)
                    .ToList();

                _logger.LogInformation("[DashboardAdminService] Found {Count} top forum categories", topForumCategories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[DashboardAdminService] Error getting top forum categories: {Message}", ex.Message);
            }

            var topContent = new TopContentDto
            {
                TopExercises = topExercises,
                TopForumCategories = topForumCategories,
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[DashboardAdminService] Top content calculated successfully");

            return (true, topContent, "Top content retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardAdminService] Error calculating top content: {Message}", ex.Message);
            return (false, null, $"Error calculating top content: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all users ordered by contribution points descending
    /// </summary>
    public async Task<(bool Success, object? Data, string Message)> GetUsersByContributionPointsAsync()
    {
        try
        {
            _logger.LogInformation("[DashboardAdminService] Getting users by contribution points");

            var userProfiles = await _dependencies.User.UserProfileRepository.GetAllUsersByContributionPointsAsync();

            var userContributions = new List<UserContributionDto>();
            int rank = 1;

            foreach (var profile in userProfiles)
            {
                var user = await _dependencies.User.UserRepository.GetByIdAsync(profile.UserId);
                if (user != null)
                {
                    userContributions.Add(new UserContributionDto
                    {
                        UserId = user.UserId,
                        FullName = profile.FullName,
                        Email = user.Email,
                        ContributionPoints = profile.ContributionPoints,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        RankTitle = null // Will be set by leaderboard service if needed
                    });
                }
                rank++;
            }

            _logger.LogInformation("[DashboardAdminService] Retrieved {Count} users by contribution points", userContributions.Count);

            return (true, userContributions, "Users retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardAdminService] Error getting users by contribution points: {Message}", ex.Message);
            return (false, null, $"Error getting users by contribution points: {ex.Message}");
        }
    }
}

