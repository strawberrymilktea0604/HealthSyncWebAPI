using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Moq;
using FluentAssertions;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class LeaderboardCalculationServiceTests
{
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<IWorkoutLogRepository> _workoutLogRepositoryMock;
    private readonly Mock<IForumPostRepository> _forumPostRepositoryMock;
    private readonly Mock<IForumReplyRepository> _forumReplyRepositoryMock;
    private readonly Mock<IChallengeParticipationRepository> _challengeParticipationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly LeaderboardCalculationService _service;

    public LeaderboardCalculationServiceTests()
    {
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _workoutLogRepositoryMock = new Mock<IWorkoutLogRepository>();
        _forumPostRepositoryMock = new Mock<IForumPostRepository>();
        _forumReplyRepositoryMock = new Mock<IForumReplyRepository>();
        _challengeParticipationRepositoryMock = new Mock<IChallengeParticipationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _service = new LeaderboardCalculationService(
            _leaderboardRepositoryMock.Object,
            _workoutLogRepositoryMock.Object,
            _forumPostRepositoryMock.Object,
            _forumReplyRepositoryMock.Object,
            _challengeParticipationRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    #region CalculateUserPointsAsync Tests

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculateCorrectPoints_WhenUserHasAllActivities()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(4); // 4 workouts * 5 = 20 points

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(3); // 3 posts * 2 = 6 points

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(10); // 10 replies * 1 = 10 points

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(2); // 2 completed challenges * 10 = 20 points

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(56); // 20 + 6 + 10 + 20

        _workoutLogRepositoryMock.Verify(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth), Times.Once);
        _forumPostRepositoryMock.Verify(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth), Times.Once);
        _forumReplyRepositoryMock.Verify(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth), Times.Once);
        _challengeParticipationRepositoryMock.Verify(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth), Times.Once);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldReturnZero_WhenUserHasNoActivities()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculateWorkoutPointsOnly()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(5); // 5 workouts * 5 = 25 points

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(25);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculatePostPointsOnly()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(7); // 7 posts * 2 = 14 points

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(14);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculateReplyPointsOnly()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(8); // 8 replies * 1 = 8 points

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(8);
    }

    [Fact]
    public async Task CalculateUserPointsAsync_ShouldCalculateChallengePointsOnly()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(3); // 3 completed challenges * 10 = 30 points

        // Act
        var result = await _service.CalculateUserPointsAsync(userId);

        // Assert
        result.Should().Be(30);
    }

    #endregion

    #region UpdateUserPointsAsync Tests

    [Fact]
    public async Task UpdateUserPointsAsync_ShouldCreateNewLeaderboardEntry_WhenUserHasNoExistingEntry()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        var expectedPoints = 42; // 2 workouts * 5 + 16 posts * 2 + 0 replies + 0 challenges = 42

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((Leaderboard?)null);

        // Mock the repositories to return values that result in expectedPoints
        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(2);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(16);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        await _service.UpdateUserPointsAsync(userId);

        // Assert
        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.Is<Leaderboard>(l =>
            l.UserId == userId &&
            l.TotalPoints == expectedPoints)), Times.Once);

        _leaderboardRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPointsAsync_ShouldUpdateExistingLeaderboardEntry()
    {
        // Arrange
        var userId = 1;
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        var expectedPoints = 42; // 2 workouts * 5 + 16 posts * 2 + 0 replies + 0 challenges = 42
        var existingLeaderboard = new Leaderboard
        {
            UserId = userId,
            TotalPoints = 10,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingLeaderboard);

        // Mock the repositories to return values that result in expectedPoints
        _workoutLogRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(2);

        _forumPostRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(16);

        _forumReplyRepositoryMock
            .Setup(r => r.CountByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        _challengeParticipationRepositoryMock
            .Setup(r => r.CountCompletedByUserIdAndMonthAsync(userId, currentYear, currentMonth))
            .ReturnsAsync(0);

        // Act
        await _service.UpdateUserPointsAsync(userId);

        // Assert
        _leaderboardRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l =>
            l.UserId == userId &&
            l.TotalPoints == expectedPoints)), Times.Once);

        _leaderboardRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Leaderboard>()), Times.Never);
    }

    #endregion

    #region UpdateAllUsersPointsAsync Tests

    [Fact]
    public async Task UpdateAllUsersPointsAsync_ShouldUpdatePointsForAllActiveUsers()
    {
        // Arrange
        var activeUsers = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, IsActive = true },
            new ApplicationUser { UserId = 2, IsActive = true },
            new ApplicationUser { UserId = 3, IsActive = true }
        };

        _userRepositoryMock
            .Setup(r => r.GetActiveUsersAsync())
            .ReturnsAsync(activeUsers);

        // Setup leaderboard entries for each user
        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync((Leaderboard?)null);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(2))
            .ReturnsAsync(new Leaderboard { UserId = 2, TotalPoints = 5 });

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(3))
            .ReturnsAsync(new Leaderboard { UserId = 3, TotalPoints = 15 });

        // Act
        await _service.UpdateAllUsersPointsAsync();

        // Assert
        _userRepositoryMock.Verify(r => r.GetActiveUsersAsync(), Times.Once);

        // Verify that UpdateUserPointsAsync was called for each user
        // (This is indirectly tested through the repository calls above)
        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.Is<Leaderboard>(l => l.UserId == 1)), Times.Once);
        _leaderboardRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => l.UserId == 2)), Times.Once);
        _leaderboardRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => l.UserId == 3)), Times.Once);
    }

    [Fact]
    public async Task UpdateAllUsersPointsAsync_ShouldHandleEmptyUserList()
    {
        // Arrange
        var activeUsers = new List<ApplicationUser>();

        _userRepositoryMock
            .Setup(r => r.GetActiveUsersAsync())
            .ReturnsAsync(activeUsers);

        // Act
        await _service.UpdateAllUsersPointsAsync();

        // Assert
        _userRepositoryMock.Verify(r => r.GetActiveUsersAsync(), Times.Once);
        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Leaderboard>()), Times.Never);
        _leaderboardRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Leaderboard>()), Times.Never);
    }

    #endregion
}

