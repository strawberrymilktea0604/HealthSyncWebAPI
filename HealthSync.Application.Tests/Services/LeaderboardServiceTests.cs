using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Leaderboard;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class LeaderboardServiceTests
{
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly LeaderboardService _service;

    public LeaderboardServiceTests()
    {
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _service = new LeaderboardService(_leaderboardRepositoryMock.Object, _userProfileRepositoryMock.Object);
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldReturnTopUsersAsDtos_WhenLimitSpecified()
    {
        // Arrange
        var limit = 10;
        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard
            {
                LeaderboardId = 1,
                UserId = 1,
                TotalPoints = 100,
                RankTitle = "Top Contributor",
                RankPosition = 1,
                UpdatedAt = DateTime.UtcNow,
                User = new ApplicationUser
                {
                    UserId = 1,
                    Email = "user1@example.com",
                    UserProfile = new UserProfile
                    {
                        FullName = "User One",
                        AvatarUrl = "avatar1.jpg"
                    }
                }
            },
            new Leaderboard
            {
                LeaderboardId = 2,
                UserId = 2,
                TotalPoints = 80,
                RankTitle = null,
                RankPosition = 2,
                UpdatedAt = DateTime.UtcNow,
                User = new ApplicationUser
                {
                    UserId = 2,
                    Email = "user2@example.com",
                    UserProfile = new UserProfile
                    {
                        FullName = "User Two",
                        AvatarUrl = null
                    }
                }
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetTopUsersAsync(limit))
            .ReturnsAsync(leaderboards);

        // Act
        var result = await _service.GetTopUsersAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var first = result.First();
        first.LeaderboardId.Should().Be(1);
        first.UserId.Should().Be(1);
        first.UserName.Should().Be("User One");
        first.AvatarUrl.Should().Be("avatar1.jpg");
        first.TotalPoints.Should().Be(100);
        first.RankTitle.Should().Be("Top Contributor");
        first.RankPosition.Should().Be(1);

        var second = result.Last();
        second.UserName.Should().Be("User Two");
        second.AvatarUrl.Should().BeNull();
        second.RankTitle.Should().BeNull();
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldUseDefaultLimit_WhenNotSpecified()
    {
        // Arrange
        var defaultLimit = 100;
        var leaderboards = new List<Leaderboard>();

        _leaderboardRepositoryMock
            .Setup(r => r.GetTopUsersAsync(defaultLimit))
            .ReturnsAsync(leaderboards);

        // Act
        var result = await _service.GetTopUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _leaderboardRepositoryMock.Verify(r => r.GetTopUsersAsync(defaultLimit), Times.Once);
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldUseEmailAsFallback_WhenUserProfileIsNull()
    {
        // Arrange
        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard
            {
                LeaderboardId = 1,
                UserId = 1,
                TotalPoints = 50,
                User = new ApplicationUser
                {
                    UserId = 1,
                    Email = "user1@example.com",
                    UserProfile = null
                }
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetTopUsersAsync(It.IsAny<int>()))
            .ReturnsAsync(leaderboards);

        // Act
        var result = await _service.GetTopUsersAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.First().UserName.Should().Be("user1@example.com");
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldUseUnknownAsFallback_WhenUserAndProfileAreNull()
    {
        // Arrange
        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard
            {
                LeaderboardId = 1,
                UserId = 1,
                TotalPoints = 50
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetTopUsersAsync(It.IsAny<int>()))
            .ReturnsAsync(leaderboards);

        // Act
        var result = await _service.GetTopUsersAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.First().UserName.Should().Be("Unknown");
    }

    [Fact]
    public async Task GetUserRankAsync_ShouldReturnUserRankDto_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var leaderboard = new Leaderboard
        {
            LeaderboardId = 1,
            UserId = userId,
            TotalPoints = 100,
            RankTitle = "Top Contributor",
            RankPosition = 1,
            UpdatedAt = DateTime.UtcNow,
            User = new ApplicationUser
            {
                UserId = userId,
                Email = "user@example.com",
                UserProfile = new UserProfile
                {
                    FullName = "Test User",
                    AvatarUrl = "avatar.jpg"
                }
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(leaderboard);

        _leaderboardRepositoryMock
            .Setup(r => r.GetHigherPointsCountAsync(leaderboard.TotalPoints))
            .ReturnsAsync(5); // 5 users have higher points

        // Act
        var result = await _service.GetUserRankAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.UserName.Should().Be("Test User");
        result.AvatarUrl.Should().Be("avatar.jpg");
        result.TotalPoints.Should().Be(100);
        result.RankPosition.Should().Be(6); // 5 higher + 1 = rank 6
        result.RankTitle.Should().Be("Top Contributor");
    }

    [Fact]
    public async Task GetUserRankAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((Leaderboard?)null);

        // Act
        var result = await _service.GetUserRankAsync(userId);

        // Assert
        result.Should().BeNull();

        _leaderboardRepositoryMock.Verify(r => r.GetHigherPointsCountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserRankAsync_ShouldCalculateRankPositionCorrectly_WhenNoHigherPoints()
    {
        // Arrange
        var userId = 1;
        var leaderboard = new Leaderboard
        {
            LeaderboardId = 1,
            UserId = userId,
            TotalPoints = 100,
            User = new ApplicationUser
            {
                UserId = userId,
                Email = "user@example.com"
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(leaderboard);

        _leaderboardRepositoryMock
            .Setup(r => r.GetHigherPointsCountAsync(leaderboard.TotalPoints))
            .ReturnsAsync(0); // No users have higher points

        // Act
        var result = await _service.GetUserRankAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.RankPosition.Should().Be(1); // First place
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldReturnPaginatedResult_WhenValidParameters()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 10;
        var totalCount = 25;

        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard
            {
                LeaderboardId = 1,
                UserId = 1,
                TotalPoints = 100,
                RankPosition = 11,
                UpdatedAt = DateTime.UtcNow,
                User = new ApplicationUser
                {
                    UserId = 1,
                    Email = "user1@example.com",
                    UserProfile = new UserProfile
                    {
                        FullName = "User One"
                    }
                }
            }
        };

        _leaderboardRepositoryMock
            .Setup(r => r.GetLeaderboardAsync(pageNumber, pageSize))
            .ReturnsAsync((leaderboards.AsEnumerable(), totalCount));

        // Act
        var result = await _service.GetLeaderboardAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(totalCount);
        result.CurrentPage.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be(3); // 25 / 10 = 3 pages

        var firstItem = result.Items.First();
        firstItem.UserId.Should().Be(1);
        firstItem.UserName.Should().Be("User One");
        firstItem.TotalPoints.Should().Be(100);
        firstItem.RankPosition.Should().Be(11);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldUseDefaultParameters_WhenNotSpecified()
    {
        // Arrange
        var defaultPageNumber = 1;
        var defaultPageSize = 20;

        _leaderboardRepositoryMock
            .Setup(r => r.GetLeaderboardAsync(defaultPageNumber, defaultPageSize))
            .ReturnsAsync((Enumerable.Empty<Leaderboard>(), 0));

        // Act
        var result = await _service.GetLeaderboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.CurrentPage.Should().Be(defaultPageNumber);
        result.PageSize.Should().Be(defaultPageSize);

        _leaderboardRepositoryMock.Verify(r => r.GetLeaderboardAsync(defaultPageNumber, defaultPageSize), Times.Once);
    }

    [Fact]
    public async Task GetTopUsersByContributionPointsAsync_ShouldReturnTopUsersByContributionPoints()
    {
        // Arrange
        var limit = 5;
        var userProfiles = new List<UserProfile>
        {
            new UserProfile
            {
                UserId = 1,
                FullName = "User One",
                AvatarUrl = "avatar1.jpg",
                ContributionPoints = 150
            },
            new UserProfile
            {
                UserId = 2,
                FullName = "User Two",
                AvatarUrl = null,
                ContributionPoints = 120
            }
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetTopUsersByContributionPointsAsync(limit))
            .ReturnsAsync(userProfiles);

        // Act
        var result = await _service.GetTopUsersByContributionPointsAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var first = result.First();
        first.UserId.Should().Be(1);
        first.FullName.Should().Be("User One");
        first.AvatarUrl.Should().Be("avatar1.jpg");
        first.ContributionPoints.Should().Be(150);

        var second = result.Last();
        second.UserId.Should().Be(2);
        second.FullName.Should().Be("User Two");
        second.AvatarUrl.Should().BeNull();
        second.ContributionPoints.Should().Be(120);
    }

    [Fact]
    public async Task GetTopUsersByContributionPointsAsync_ShouldUseDefaultLimit_WhenNotSpecified()
    {
        // Arrange
        var defaultLimit = 100;

        _userProfileRepositoryMock
            .Setup(r => r.GetTopUsersByContributionPointsAsync(defaultLimit))
            .ReturnsAsync(new List<UserProfile>());

        // Act
        var result = await _service.GetTopUsersByContributionPointsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _userProfileRepositoryMock.Verify(r => r.GetTopUsersByContributionPointsAsync(defaultLimit), Times.Once);
    }
}