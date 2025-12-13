using FluentAssertions;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();

        _service = new UserService(
            _userRepositoryMock.Object,
            _leaderboardRepositoryMock.Object,
            _userProfileRepositoryMock.Object);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_ShouldUpdateStatus_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "test@example.com",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        await _service.UpdateUserStatusAsync(userId, false);

        // Assert
        user.IsActive.Should().BeFalse();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_ShouldThrowKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateUserStatusAsync(userId, false));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldUpdateRole_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "test@example.com",
            Role = "Customer"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        await _service.UpdateUserRoleAsync(userId, "Admin");

        // Assert
        user.Role.Should().Be("Admin");
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldThrowKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateUserRoleAsync(userId, "Admin"));
    }

    [Fact]
    public async Task SetUserRankTitleAsync_ShouldUpdateRankTitleAndReturnDto_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var rankTitle = "Top Contributor";

        var user = new ApplicationUser { UserId = userId, Email = "test@example.com" };
        var userProfile = new UserProfile { UserId = userId, FullName = "John Doe" };
        var leaderboard = new Leaderboard
        {
            UserId = userId,
            RankTitle = rankTitle,
            TotalPoints = 150,
            RankPosition = 5,
            UpdatedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _leaderboardRepositoryMock
            .Setup(r => r.SetRankTitleAsync(userId, rankTitle))
            .ReturnsAsync(true);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(leaderboard);

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        // Act
        var result = await _service.SetUserRankTitleAsync(userId, rankTitle);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result!.FullName.Should().Be("John Doe");
        result!.RankTitle.Should().Be(rankTitle);
        result!.TotalPoints.Should().Be(150);
        result!.RankPosition.Should().Be(5);
    }

    [Fact]
    public async Task SetUserRankTitleAsync_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.SetUserRankTitleAsync(userId, "Top Contributor");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetUserRankTitleAsync_ShouldReturnNull_WhenSetRankTitleFails()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser { UserId = userId, Email = "test@example.com" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _leaderboardRepositoryMock
            .Setup(r => r.SetRankTitleAsync(userId, "Top Contributor"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SetUserRankTitleAsync(userId, "Top Contributor");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetUserRankTitleAsync_ShouldHandleNullRankTitle()
    {
        // Arrange
        var userId = 1;

        var user = new ApplicationUser { UserId = userId, Email = "test@example.com" };
        var userProfile = new UserProfile { UserId = userId, FullName = "John Doe" };
        var leaderboard = new Leaderboard
        {
            UserId = userId,
            RankTitle = null,
            TotalPoints = 100,
            RankPosition = 10,
            UpdatedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _leaderboardRepositoryMock
            .Setup(r => r.SetRankTitleAsync(userId, null))
            .ReturnsAsync(true);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(leaderboard);

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        // Act
        var result = await _service.SetUserRankTitleAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result!.RankTitle.Should().BeNull();
    }
}