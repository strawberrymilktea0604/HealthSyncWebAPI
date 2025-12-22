using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HealthSync.Application.Tests.Services;

public class PointCalculationServiceTests
{
    private readonly Mock<IWorkoutLogRepository> _workoutLogRepositoryMock;
    private readonly Mock<IForumPostRepository> _forumPostRepositoryMock;
    private readonly Mock<IForumReplyRepository> _forumReplyRepositoryMock;
    private readonly Mock<IChallengeParticipationRepository> _challengeParticipationRepositoryMock;
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<PointCalculationService>> _loggerMock;
    private readonly PointCalculationService _service;

    public PointCalculationServiceTests()
    {
        _workoutLogRepositoryMock = new Mock<IWorkoutLogRepository>();
        _forumPostRepositoryMock = new Mock<IForumPostRepository>();
        _forumReplyRepositoryMock = new Mock<IForumReplyRepository>();
        _challengeParticipationRepositoryMock = new Mock<IChallengeParticipationRepository>();
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<PointCalculationService>>();

        _service = new PointCalculationService(
            _workoutLogRepositoryMock.Object,
            _forumPostRepositoryMock.Object,
            _forumReplyRepositoryMock.Object,
            _challengeParticipationRepositoryMock.Object,
            _leaderboardRepositoryMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculateCorrectPoints_WhenUserHasActivities()
    {
        // Arrange
        var userId = 1;

        // Mock workout logs count (5 points each)
        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null, null))
            .ReturnsAsync(new HealthSync.Application.DTOs.PaginatedResult<WorkoutLog>
            {
                Items = new List<WorkoutLog>(),
                TotalItems = 3, // 3 workouts = 15 points
                CurrentPage = 1,
                PageSize = 1,
                TotalPages = 3
            });

        // Mock forum posts count (2 points each)
        var posts = new List<Post>
        {
            new Post { PostId = 1, UserId = userId, Title = "Post 1" },
            new Post { PostId = 2, UserId = userId, Title = "Post 2" }
        };
        _forumPostRepositoryMock
            .Setup(r => r.GetAllPostsAsync())
            .ReturnsAsync(posts); // 2 posts = 4 points

        // Mock forum replies count (1 point each)
        var replies = new List<Reply>
        {
            new Reply { ReplyId = 1, UserId = userId, Content = "Reply 1" },
            new Reply { ReplyId = 2, UserId = userId, Content = "Reply 2" },
            new Reply { ReplyId = 3, UserId = userId, Content = "Reply 3" }
        };
        _forumReplyRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(replies); // 3 replies = 3 points

        // Mock completed challenges count (10 points each)
        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation { ParticipationId = 1, UserId = userId, Status = ParticipationStatus.Completed },
            new ChallengeParticipation { ParticipationId = 2, UserId = userId, Status = ParticipationStatus.Completed }
        };
        _challengeParticipationRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(participations); // 2 completed challenges = 20 points

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(42); // 15 + 4 + 3 + 20 = 42 points
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldReturnZero_WhenUserHasNoActivities()
    {
        // Arrange
        var userId = 1;

        // Mock empty results
        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null, null))
            .ReturnsAsync(new HealthSync.Application.DTOs.PaginatedResult<WorkoutLog>
            {
                Items = new List<WorkoutLog>(),
                TotalItems = 0,
                CurrentPage = 1,
                PageSize = 1,
                TotalPages = 0
            });

        _forumPostRepositoryMock
            .Setup(r => r.GetAllPostsAsync())
            .ReturnsAsync(new List<Post>());

        _forumReplyRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Reply>());

        _challengeParticipationRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ChallengeParticipation>());

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldOnlyCountCompletedChallenges()
    {
        // Arrange
        var userId = 1;

        // Mock workout logs
        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null, null))
            .ReturnsAsync(new HealthSync.Application.DTOs.PaginatedResult<WorkoutLog>
            {
                Items = new List<WorkoutLog>(),
                TotalItems = 1, // 1 workout = 5 points
                CurrentPage = 1,
                PageSize = 1,
                TotalPages = 1
            });

        // Mock empty posts and replies
        _forumPostRepositoryMock
            .Setup(r => r.GetAllPostsAsync())
            .ReturnsAsync(new List<Post>());

        _forumReplyRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Reply>());

        // Mock challenges with mixed statuses
        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation { ParticipationId = 1, UserId = userId, Status = ParticipationStatus.Completed }, // 10 points
            new ChallengeParticipation { ParticipationId = 2, UserId = userId, Status = ParticipationStatus.PendingApproval }, // 0 points
            new ChallengeParticipation { ParticipationId = 3, UserId = userId, Status = ParticipationStatus.Failed }, // 0 points
            new ChallengeParticipation { ParticipationId = 4, UserId = 2, Status = ParticipationStatus.Completed } // Different user
        };
        _challengeParticipationRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(participations);

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(15); // 5 (workout) + 10 (completed challenge) = 15 points
    }

    [Fact]
    public async Task CalculateAndUpdateAllUserPointsAsync_ShouldUpdateExistingLeaderboardEntry()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, Email = "user1@test.com" },
            new ApplicationUser { UserId = 2, Email = "user2@test.com" }
        };

        var existingLeaderboard = new Leaderboard
        {
            LeaderboardId = 1,
            UserId = 1,
            TotalPoints = 5, // Different from calculated points (10)
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Mock point calculations
        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<int>(), It.IsAny<int>(), null, null))
            .ReturnsAsync(new HealthSync.Application.DTOs.PaginatedResult<WorkoutLog>
            {
                TotalItems = 2, // 10 points
                CurrentPage = 1,
                PageSize = 1,
                TotalPages = 2
            });

        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(2, It.IsAny<int>(), It.IsAny<int>(), null, null))
            .ReturnsAsync(new HealthSync.Application.DTOs.PaginatedResult<WorkoutLog>
            {
                TotalItems = 1, // 5 points
                CurrentPage = 1,
                PageSize = 1,
                TotalPages = 1
            });

        // Mock empty other activities
        _forumPostRepositoryMock.Setup(r => r.GetAllPostsAsync()).ReturnsAsync(new List<Post>());
        _forumReplyRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Reply>());
        _challengeParticipationRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChallengeParticipation>());

        // Mock leaderboard operations
        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(existingLeaderboard);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(2))
            .ReturnsAsync((Leaderboard?)null);

        _leaderboardRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.CalculateAndUpdateAllUserPointsAsync();

        // Assert
        result.Should().Be(2); // 2 users updated

        // Verify existing entry was updated
        _leaderboardRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l =>
            l.UserId == 1 && l.TotalPoints == 10)), Times.Once);

        // Verify new entry was created for user 2
        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.Is<Leaderboard>(l =>
            l.UserId == 2 && l.TotalPoints == 5)), Times.Once);
    }
}

