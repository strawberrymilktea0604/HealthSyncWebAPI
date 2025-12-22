using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.Infrastructure.Tests.BackgroundJobs;

public class LeaderboardUpdateJobTests
{
    private readonly Mock<IPointCalculationService> _mockPointCalculationService;
    private readonly Mock<ILeaderboardRepository> _mockLeaderboardRepository;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
    private readonly Mock<ILogger<LeaderboardUpdateJob>> _mockLogger;
    private readonly LeaderboardUpdateJob _job;

    public LeaderboardUpdateJobTests()
    {
        _mockPointCalculationService = new Mock<IPointCalculationService>();
        _mockLeaderboardRepository = new Mock<ILeaderboardRepository>();
        _mockUserProfileRepository = new Mock<IUserProfileRepository>();
        _mockLogger = new Mock<ILogger<LeaderboardUpdateJob>>();
        
        _job = new LeaderboardUpdateJob(
            _mockPointCalculationService.Object,
            _mockLeaderboardRepository.Object,
            _mockUserProfileRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_AllUsers_UpdatesSuccessfully()
    {
        // Arrange
        _mockPointCalculationService.Setup(s => s.CalculateAndUpdateAllUserPointsAsync()).ReturnsAsync(5);
        
        var leaderboardEntries = new List<Leaderboard>
        {
            new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100 },
            new Leaderboard { LeaderboardId = 2, UserId = 2, TotalPoints = 200 }
        };
        _mockLeaderboardRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(leaderboardEntries);
        
        var userProfile1 = new UserProfile { UserProfileId = 1, UserId = 1, ContributionPoints = 90 };
        var userProfile2 = new UserProfile { UserProfileId = 2, UserId = 2, ContributionPoints = 190 };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(userProfile1);
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(userProfile2);

        // Act
        await _job.UpdateUserContributionPointsAsync();

        // Assert
        _mockPointCalculationService.Verify(s => s.CalculateAndUpdateAllUserPointsAsync(), Times.Once);
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Exactly(2));
        _mockUserProfileRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_AllUsers_LogsInformation()
    {
        // Arrange
        _mockPointCalculationService.Setup(s => s.CalculateAndUpdateAllUserPointsAsync()).ReturnsAsync(3);
        _mockLeaderboardRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Leaderboard>());

        // Act
        await _job.UpdateUserContributionPointsAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting complete point calculation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_AllUsers_ThrowsException_LogsError()
    {
        // Arrange
        _mockPointCalculationService.Setup(s => s.CalculateAndUpdateAllUserPointsAsync()).ThrowsAsync(new Exception("Test error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.UpdateUserContributionPointsAsync());
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fatal error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_CreatesLeaderboardEntry()
    {
        // Arrange
        var userId = 123;
        var calculatedPoints = 150;
        
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(calculatedPoints);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Leaderboard?)null);
        _mockLeaderboardRepository.Setup(r => r.AddAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var userProfile = new UserProfile { UserProfileId = 1, UserId = userId, ContributionPoints = 0 };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockLeaderboardRepository.Verify(r => r.AddAsync(It.Is<Leaderboard>(l => 
            l.UserId == userId && 
            l.TotalPoints == calculatedPoints)), Times.Once);
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(up => 
            up.ContributionPoints == calculatedPoints)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_UpdatesExistingLeaderboard()
    {
        // Arrange
        var userId = 123;
        var oldPoints = 100;
        var newPoints = 150;
        
        var existingLeaderboard = new Leaderboard { LeaderboardId = 1, UserId = userId, TotalPoints = oldPoints };
        
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(newPoints);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingLeaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var userProfile = new UserProfile { UserProfileId = 1, UserId = userId, ContributionPoints = oldPoints };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => 
            l.TotalPoints == newPoints)), Times.Once);
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(up => 
            up.ContributionPoints == newPoints)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_PointsUnchanged_DoesNotUpdate()
    {
        // Arrange
        var userId = 123;
        var points = 100;
        
        var existingLeaderboard = new Leaderboard { LeaderboardId = 1, UserId = userId, TotalPoints = points };
        
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(points);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingLeaderboard);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var userProfile = new UserProfile { UserProfileId = 1, UserId = userId, ContributionPoints = points };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.IsAny<Leaderboard>()), Times.Never);
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_UserProfileNotFound_DoesNotUpdateProfile()
    {
        // Arrange
        var userId = 123;
        var calculatedPoints = 150;
        
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(calculatedPoints);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Leaderboard?)null);
        _mockLeaderboardRepository.Setup(r => r.AddAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
        _mockLeaderboardRepository.Verify(r => r.AddAsync(It.IsAny<Leaderboard>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_LogsInformation()
    {
        // Arrange
        var userId = 123;
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(100);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Leaderboard?)null);
        _mockLeaderboardRepository.Setup(r => r.AddAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"UserId: {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 123;
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ThrowsAsync(new Exception("Test error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.UpdateUserContributionPointsAsync(userId));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"UserId {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_AllUsers_SyncToUserProfile_SkipsUserWithoutProfile()
    {
        // Arrange
        _mockPointCalculationService.Setup(s => s.CalculateAndUpdateAllUserPointsAsync()).ReturnsAsync(2);
        
        var leaderboardEntries = new List<Leaderboard>
        {
            new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100 },
            new Leaderboard { LeaderboardId = 2, UserId = 2, TotalPoints = 200 }
        };
        _mockLeaderboardRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(leaderboardEntries);
        
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserProfile?)null);
        var userProfile2 = new UserProfile { UserProfileId = 2, UserId = 2, ContributionPoints = 190 };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(userProfile2);

        // Act
        await _job.UpdateUserContributionPointsAsync();

        // Assert
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UserProfile not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_AllUsers_SyncContinuesOnError()
    {
        // Arrange
        _mockPointCalculationService.Setup(s => s.CalculateAndUpdateAllUserPointsAsync()).ReturnsAsync(3);
        
        var leaderboardEntries = new List<Leaderboard>
        {
            new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100 },
            new Leaderboard { LeaderboardId = 2, UserId = 2, TotalPoints = 200 },
            new Leaderboard { LeaderboardId = 3, UserId = 3, TotalPoints = 300 }
        };
        _mockLeaderboardRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(leaderboardEntries);
        
        var userProfile1 = new UserProfile { UserProfileId = 1, UserId = 1, ContributionPoints = 90 };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(userProfile1);
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(2)).ThrowsAsync(new Exception("Database error"));
        var userProfile3 = new UserProfile { UserProfileId = 3, UserId = 3, ContributionPoints = 290 };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(3)).ReturnsAsync(userProfile3);

        // Act
        await _job.UpdateUserContributionPointsAsync();

        // Assert
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Exactly(2));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error syncing UserId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPointsAsync_SpecificUser_UpdatesTimestamps()
    {
        // Arrange
        var userId = 123;
        var calculatedPoints = 150;
        var oldTimestamp = DateTime.UtcNow.AddDays(-1);
        
        var existingLeaderboard = new Leaderboard 
        { 
            LeaderboardId = 1, 
            UserId = userId, 
            TotalPoints = 100,
            UpdatedAt = oldTimestamp
        };
        
        _mockPointCalculationService.Setup(s => s.CalculateUserPointsAsync(userId)).ReturnsAsync(calculatedPoints);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingLeaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);
        _mockLeaderboardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var userProfile = new UserProfile 
        { 
            UserProfileId = 1, 
            UserId = userId, 
            ContributionPoints = 100,
            UpdatedAt = oldTimestamp
        };
        _mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        // Act
        await _job.UpdateUserContributionPointsAsync(userId);

        // Assert
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => 
            l.UpdatedAt > oldTimestamp)), Times.Once);
        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(up => 
            up.UpdatedAt > oldTimestamp)), Times.Once);
    }
}


