using FluentAssertions;
using HealthSync.Application.DTOs.Dashboard;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class DashboardAdminServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IWorkoutLogRepository> _workoutLogRepositoryMock;
    private readonly Mock<INutritionLogRepository> _nutritionLogRepositoryMock;
    private readonly Mock<IForumPostRepository> _forumPostRepositoryMock;
    private readonly Mock<IForumReplyRepository> _forumReplyRepositoryMock;
    private readonly Mock<IChallengeRepository> _challengeRepositoryMock;
    private readonly Mock<IChallengeParticipationRepository> _participationRepositoryMock;
    private readonly Mock<IExerciseRepository> _exerciseRepositoryMock;
    private readonly Mock<IForumCategoryRepository> _forumCategoryRepositoryMock;
    private readonly Mock<IExerciseSessionRepository> _exerciseSessionRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly Mock<ILogger<DashboardAdminService>> _loggerMock;
    private readonly DashboardAdminService _service;

    public DashboardAdminServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _workoutLogRepositoryMock = new Mock<IWorkoutLogRepository>();
        _nutritionLogRepositoryMock = new Mock<INutritionLogRepository>();
        _forumPostRepositoryMock = new Mock<IForumPostRepository>();
        _forumReplyRepositoryMock = new Mock<IForumReplyRepository>();
        _challengeRepositoryMock = new Mock<IChallengeRepository>();
        _participationRepositoryMock = new Mock<IChallengeParticipationRepository>();
        _exerciseRepositoryMock = new Mock<IExerciseRepository>();
        _forumCategoryRepositoryMock = new Mock<IForumCategoryRepository>();
        _exerciseSessionRepositoryMock = new Mock<IExerciseSessionRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _loggerMock = new Mock<ILogger<DashboardAdminService>>();

        _service = new DashboardAdminService(
            _userRepositoryMock.Object,
            _workoutLogRepositoryMock.Object,
            _nutritionLogRepositoryMock.Object,
            _forumPostRepositoryMock.Object,
            _forumReplyRepositoryMock.Object,
            _challengeRepositoryMock.Object,
            _participationRepositoryMock.Object,
            _exerciseRepositoryMock.Object,
            _forumCategoryRepositoryMock.Object,
            _exerciseSessionRepositoryMock.Object,
            _userProfileRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturnSuccessWithStats_WhenAllDataAvailable()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new ApplicationUser { UserId = 2, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new ApplicationUser { UserId = 3, IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        _workoutLogRepositoryMock
            .Setup(r => r.CountWorkoutLogsTodayAsync())
            .ReturnsAsync(15);

        // Act
        var result = await _service.GetDashboardStatsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Dashboard statistics retrieved successfully");

        var stats = result.Data as DashboardStatsDto;
        stats.Should().NotBeNull();
        stats!.TotalActiveUsers.Should().Be(2); // 2 active users
        stats.NewUsersThisMonth.Should().Be(3); // 3 users created this month
        stats.WorkoutLogsToday.Should().Be(15);
        stats.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetDashboardStatsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("Error calculating dashboard statistics");
    }

    [Fact]
    public async Task GetDetailedStatsAsync_ShouldReturnSuccessWithDetailedStats_WhenAllDataAvailable()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, IsActive = true, CreatedAt = firstDayOfMonth.AddDays(5) },
            new ApplicationUser { UserId = 2, IsActive = true, CreatedAt = firstDayOfMonth.AddDays(10) }
        };

        var nutritionLogs = new List<NutritionLog>
        {
            new NutritionLog { NutritionLogId = 1, LogDate = today },
            new NutritionLog { NutritionLogId = 2, LogDate = today }
        };

        var posts = new List<Post>
        {
            new Post { PostId = 1, CreatedAt = firstDayOfMonth.AddDays(5) },
            new Post { PostId = 2, CreatedAt = firstDayOfMonth.AddDays(10) }
        };

        var replies = new List<Reply>
        {
            new Reply { ReplyId = 1, CreatedAt = firstDayOfMonth.AddDays(7) }
        };

        var challenges = new List<Challenge>
        {
            new Challenge { ChallengeId = 1, Status = ChallengeStatus.Open },
            new Challenge { ChallengeId = 2, Status = ChallengeStatus.Closed }
        };

        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation { ParticipationId = 1, Status = ParticipationStatus.PendingApproval },
            new ChallengeParticipation { ParticipationId = 2, Status = ParticipationStatus.Completed }
        };

        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _workoutLogRepositoryMock.Setup(r => r.CountWorkoutLogsTodayAsync()).ReturnsAsync(8);
        _nutritionLogRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(nutritionLogs);
        _forumPostRepositoryMock.Setup(r => r.GetAllPostsAsync()).ReturnsAsync(posts);
        _forumReplyRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(replies);
        _challengeRepositoryMock.Setup(r => r.GetAllAsync(1, int.MaxValue)).ReturnsAsync((challenges, 2));
        _participationRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(participations);

        // Act
        var result = await _service.GetDetailedStatsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Detailed statistics retrieved successfully");

        var stats = result.Data as DetailedDashboardStatsDto;
        stats.Should().NotBeNull();
        stats!.TotalActiveUsers.Should().Be(2);
        stats.NewUsersThisMonth.Should().Be(2);
        stats.WorkoutLogsToday.Should().Be(8);
        stats.NutritionLogsToday.Should().Be(2);
        stats.ForumPostsThisMonth.Should().Be(2);
        stats.ForumRepliesThisMonth.Should().Be(1);
        stats.OpenChallenges.Should().Be(1);
        stats.PendingChallengeSubmissions.Should().Be(1);
    }

    [Fact]
    public async Task GetDetailedStatsAsync_ShouldHandleRepositoryExceptionsGracefully()
    {
        // Arrange - Setup all repositories to throw exceptions
        _userRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _workoutLogRepositoryMock.Setup(r => r.CountWorkoutLogsTodayAsync()).ThrowsAsync(new Exception("DB Error"));
        _nutritionLogRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _forumPostRepositoryMock.Setup(r => r.GetAllPostsAsync()).ThrowsAsync(new Exception("DB Error"));
        _forumReplyRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _challengeRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB Error"));
        _participationRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));

        // Act
        var result = await _service.GetDetailedStatsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("Error calculating statistics");
    }

    [Fact]
    public async Task GetTopContentAsync_ShouldReturnSuccessWithTopContent_WhenDataAvailable()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise { ExerciseId = 1, Name = "Push-ups", MuscleGroup = MuscleGroup.Chest, DifficultyLevel = DifficultyLevel.Beginner },
            new Exercise { ExerciseId = 2, Name = "Squats", MuscleGroup = MuscleGroup.Legs, DifficultyLevel = DifficultyLevel.Intermediate }
        };

        var sessions = new List<ExerciseSession>
        {
            new ExerciseSession { ExerciseSessionId = 1, ExerciseId = 1 }, // Push-ups used once
            new ExerciseSession { ExerciseSessionId = 2, ExerciseId = 1 }, // Push-ups used twice
            new ExerciseSession { ExerciseSessionId = 3, ExerciseId = 1 }, // Push-ups used three times
            new ExerciseSession { ExerciseSessionId = 4, ExerciseId = 2 }  // Squats used once
        };

        var categories = new List<ForumCategory>
        {
            new ForumCategory { CategoryId = 1, Name = "General" },
            new ForumCategory { CategoryId = 2, Name = "Workout Tips" }
        };

        var posts = new List<Post>
        {
            new Post { PostId = 1, CategoryId = 1 },
            new Post { PostId = 2, CategoryId = 1 },
            new Post { PostId = 3, CategoryId = 2 }
        };

        var replies = new List<Reply>
        {
            new Reply { ReplyId = 1, PostId = 1 }, // Reply to post in General
            new Reply { ReplyId = 2, PostId = 2 }  // Reply to post in General
        };

        _exerciseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(exercises);
        _exerciseSessionRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(sessions);
        _forumCategoryRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
        _forumPostRepositoryMock.Setup(r => r.GetAllPostsAsync()).ReturnsAsync(posts);
        _forumReplyRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(replies);

        // Act
        var result = await _service.GetTopContentAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Top content retrieved successfully");

        var topContent = result.Data as TopContentDto;
        topContent.Should().NotBeNull();
        topContent!.TopExercises.Should().HaveCount(2);
        topContent.TopForumCategories.Should().HaveCount(2);

        // Push-ups should be first (3 usages)
        var firstExercise = topContent.TopExercises.First();
        firstExercise.ExerciseId.Should().Be(1);
        firstExercise.Name.Should().Be("Push-ups");
        firstExercise.UsageCount.Should().Be(3);

        // General category should be first (3 total activity: 2 posts + 1 reply)
        var firstCategory = topContent.TopForumCategories.First();
        firstCategory.CategoryId.Should().Be(1);
        firstCategory.Name.Should().Be("General");
        firstCategory.TotalActivity.Should().Be(4); // 2 posts + 2 replies
    }

    [Fact]
    public async Task GetTopContentAsync_ShouldHandleRepositoryExceptionsGracefully()
    {
        // Arrange - Setup all repositories to throw exceptions
        _exerciseRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _exerciseSessionRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _forumCategoryRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
        _forumPostRepositoryMock.Setup(r => r.GetAllPostsAsync()).ThrowsAsync(new Exception("DB Error"));
        _forumReplyRepositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));

        // Act
        var result = await _service.GetTopContentAsync();

        // Assert - Service should handle exceptions gracefully and return success with empty data
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Top content retrieved successfully");

        var topContent = result.Data as TopContentDto;
        topContent.Should().NotBeNull();
        topContent!.TopExercises.Should().BeEmpty();
        topContent.TopForumCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersByContributionPointsAsync_ShouldReturnSuccessWithUsersOrderedByPoints()
    {
        // Arrange
        var userProfiles = new List<UserProfile>
        {
            new UserProfile { UserId = 1, FullName = "User One", ContributionPoints = 50 },
            new UserProfile { UserId = 2, FullName = "User Two", ContributionPoints = 100 }
        };

        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, Email = "user1@example.com", Role = "Customer", IsActive = true },
            new ApplicationUser { UserId = 2, Email = "user2@example.com", Role = "Admin", IsActive = true }
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetAllUsersByContributionPointsAsync())
            .ReturnsAsync(userProfiles);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(users[0]);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(users[1]);

        // Act
        var result = await _service.GetUsersByContributionPointsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Users retrieved successfully");

        var userContributions = result.Data as List<UserContributionDto>;
        userContributions.Should().NotBeNull();
        userContributions!.Should().HaveCount(2);

        // Should be ordered by contribution points descending
        var firstUser = userContributions!.First();
        firstUser.UserId.Should().Be(1);
        firstUser.FullName.Should().Be("User One");
        firstUser.ContributionPoints.Should().Be(50);
        firstUser.Role.Should().Be("Customer");

        var secondUser = userContributions!.Last();
        secondUser.UserId.Should().Be(2);
        secondUser.FullName.Should().Be("User Two");
        secondUser.ContributionPoints.Should().Be(100);
        secondUser.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetUsersByContributionPointsAsync_ShouldSkipUsersWithoutMatchingUserEntity()
    {
        // Arrange
        var userProfiles = new List<UserProfile>
        {
            new UserProfile { UserId = 1, FullName = "User One", ContributionPoints = 50 },
            new UserProfile { UserId = 999, FullName = "Missing User", ContributionPoints = 25 } // No matching user
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetAllUsersByContributionPointsAsync())
            .ReturnsAsync(userProfiles);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new ApplicationUser { UserId = 1, Email = "user1@example.com", Role = "Customer", IsActive = true });

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.GetUsersByContributionPointsAsync();

        // Assert
        result.Success.Should().BeTrue();

        var userContributions = result.Data as List<UserContributionDto>;
        userContributions.Should().NotBeNull();
        userContributions!.Should().HaveCount(1); // Only one user should be included
        userContributions!.First().UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUsersByContributionPointsAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        _userProfileRepositoryMock
            .Setup(r => r.GetAllUsersByContributionPointsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetUsersByContributionPointsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("Error getting users by contribution points");
    }
}