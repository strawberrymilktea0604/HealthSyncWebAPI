using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class LeaderboardRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LeaderboardRepository _repository;

    public LeaderboardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new LeaderboardRepository(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                UserId = 1,
                Email = "user1@example.com",
                PasswordHash = "hash1",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                UserId = 2,
                Email = "user2@example.com",
                PasswordHash = "hash2",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                UserId = 3,
                Email = "user3@example.com",
                PasswordHash = "hash3",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        var profiles = new List<UserProfile>
        {
            new UserProfile
            {
                UserProfileId = 1,
                UserId = 1,
                FullName = "User One",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                HeightCm = 175,
                CurrentWeightKg = 70,
                ActivityLevel = ActivityLevel.ModeratelyActive
            },
            new UserProfile
            {
                UserProfileId = 2,
                UserId = 2,
                FullName = "User Two",
                DateOfBirth = new DateTime(1992, 5, 15),
                Gender = Gender.Female,
                HeightCm = 165,
                CurrentWeightKg = 60,
                ActivityLevel = ActivityLevel.LightlyActive
            },
            new UserProfile
            {
                UserProfileId = 3,
                UserId = 3,
                FullName = "User Three",
                DateOfBirth = new DateTime(1988, 12, 10),
                Gender = Gender.Male,
                HeightCm = 180,
                CurrentWeightKg = 80,
                ActivityLevel = ActivityLevel.VeryActive
            }
        };

        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard
            {
                LeaderboardId = 1,
                UserId = 1,
                TotalPoints = 150,
                RankTitle = "Rising Star",
                RankPosition = 2,
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Leaderboard
            {
                LeaderboardId = 2,
                UserId = 2,
                TotalPoints = 200,
                RankTitle = "Top Contributor",
                RankPosition = 1,
                UpdatedAt = DateTime.UtcNow.AddHours(-12)
            },
            new Leaderboard
            {
                LeaderboardId = 3,
                UserId = 3,
                TotalPoints = 100,
                RankTitle = null,
                RankPosition = 3,
                UpdatedAt = DateTime.UtcNow.AddHours(-6)
            }
        };

        _context.ApplicationUsers.AddRange(users);
        _context.UserProfiles.AddRange(profiles);
        _context.Leaderboards.AddRange(leaderboards);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewLeaderboardEntry()
    {
        // Arrange
        var newLeaderboard = new Leaderboard
        {
            UserId = 4,
            TotalPoints = 50,
            RankTitle = "Newcomer",
            RankPosition = 4
        };

        // Act
        await _repository.AddAsync(newLeaderboard);

        // Assert
        var savedLeaderboard = await _context.Leaderboards.FindAsync(newLeaderboard.LeaderboardId);
        savedLeaderboard.Should().NotBeNull();
        savedLeaderboard!.UserId.Should().Be(4);
        savedLeaderboard.TotalPoints.Should().Be(50);
        savedLeaderboard.RankTitle.Should().Be("Newcomer");
        savedLeaderboard.RankPosition.Should().Be(4);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnLeaderboard_WhenExists()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.LeaderboardId.Should().Be(1);
        result.UserId.Should().Be(1);
        result.TotalPoints.Should().Be(150);
        result.RankTitle.Should().Be("Rising Star");
        result.RankPosition.Should().Be(2);
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("user1@example.com");
        result.User.UserProfile.Should().NotBeNull();
        result.User!.UserProfile!.FullName.Should().Be("User One");
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllLeaderboardEntries()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingLeaderboard()
    {
        // Arrange
        var leaderboard = await _repository.GetByUserIdAsync(1);
        leaderboard!.TotalPoints = 175;
        leaderboard.RankPosition = 1;
        var originalUpdatedAt = leaderboard.UpdatedAt;

        // Act
        await _repository.UpdateAsync(leaderboard);

        // Assert
        var updatedLeaderboard = await _repository.GetByUserIdAsync(1);
        updatedLeaderboard.Should().NotBeNull();
        updatedLeaderboard!.TotalPoints.Should().Be(175);
        updatedLeaderboard.RankPosition.Should().Be(1);
        // Note: UpdatedAt is not automatically set in UpdateAsync
    }

    [Fact]
    public async Task SetRankTitleAsync_ShouldSetRankTitleAndReturnTrue_WhenUserExists()
    {
        // Act
        var result = await _repository.SetRankTitleAsync(1, "Champion");

        // Assert
        result.Should().BeTrue();

        var updatedLeaderboard = await _repository.GetByUserIdAsync(1);
        updatedLeaderboard.Should().NotBeNull();
        updatedLeaderboard!.RankTitle.Should().Be("Champion");
        updatedLeaderboard.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetRankTitleAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.SetRankTitleAsync(999, "Champion");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetRankTitleAsync_ShouldSetRankTitleToNull()
    {
        // Act
        var result = await _repository.SetRankTitleAsync(2, null);

        // Assert
        result.Should().BeTrue();

        var updatedLeaderboard = await _repository.GetByUserIdAsync(2);
        updatedLeaderboard.Should().NotBeNull();
        updatedLeaderboard!.RankTitle.Should().BeNull();
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldReturnTopUsersOrderedByPoints()
    {
        // Act
        var result = await _repository.GetTopUsersAsync(2);

        // Assert
        result.Should().NotBeNull();
        var topUsers = result.ToList();
        topUsers.Should().HaveCount(2);
        topUsers[0].TotalPoints.Should().Be(200); // User 2
        topUsers[0].User.Email.Should().Be("user2@example.com");
        topUsers[1].TotalPoints.Should().Be(150); // User 1
        topUsers[1].User.Email.Should().Be("user1@example.com");
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldReturnAllUsers_WhenLimitExceedsCount()
    {
        // Act
        var result = await _repository.GetTopUsersAsync(10);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetHigherPointsCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.GetHigherPointsCountAsync(120);

        // Assert
        result.Should().Be(2); // Users with 150 and 200 points
    }

    [Fact]
    public async Task GetHigherPointsCountAsync_ShouldReturnZero_WhenNoHigherPoints()
    {
        // Act
        var result = await _repository.GetHigherPointsCountAsync(250);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldReturnPaginatedResults()
    {
        // Act
        var (items, totalCount) = await _repository.GetLeaderboardAsync(1, 2);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(2);
        var leaderboardItems = items.ToList();
        leaderboardItems[0].TotalPoints.Should().Be(200); // First page, highest points
        leaderboardItems[1].TotalPoints.Should().Be(150);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldReturnSecondPage()
    {
        // Act
        var (items, totalCount) = await _repository.GetLeaderboardAsync(2, 2);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(1); // Only one item on second page
        var leaderboardItems = items.ToList();
        leaderboardItems[0].TotalPoints.Should().Be(100); // Third place
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveChanges()
    {
        // Arrange
        var leaderboard = new Leaderboard
        {
            UserId = 5,
            TotalPoints = 25
        };
        _context.Leaderboards.Add(leaderboard);

        // Act
        await _repository.SaveChangesAsync();

        // Assert
        leaderboard.LeaderboardId.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

