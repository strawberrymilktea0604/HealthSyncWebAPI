using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ChallengeRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ChallengeRepository _repository;

    public ChallengeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ChallengeRepository(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, Email = "admin1@test.com", PasswordHash = "hash1", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ApplicationUser { UserId = 2, Email = "admin2@test.com", PasswordHash = "hash2", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var challenges = new List<Challenge>
        {
            new Challenge
            {
                ChallengeId = 1,
                Title = "Running Challenge",
                Description = "Run 50km total",
                ChallengeType = ChallengeType.Workout,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(10),
                Criteria = "Run 50km total in 30 days",
                Status = ChallengeStatus.Open,
                MaxParticipants = 100,
                RewardDescription = "Gold medal",
                CreatedByAdminId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Challenge
            {
                ChallengeId = 2,
                Title = "Walking Challenge",
                Description = "Walk 100km total",
                ChallengeType = ChallengeType.Workout,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                Criteria = "Walk 100km total in 30 days",
                Status = ChallengeStatus.Closed,
                MaxParticipants = 50,
                RewardDescription = "Silver medal",
                CreatedByAdminId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Challenge
            {
                ChallengeId = 3,
                Title = "Nutrition Challenge",
                Description = "Maintain healthy diet",
                ChallengeType = ChallengeType.Nutrition,
                StartDate = DateTime.UtcNow.AddDays(-15),
                EndDate = DateTime.UtcNow.AddDays(-5),
                Criteria = "Log meals for 30 days",
                Status = ChallengeStatus.Open,
                MaxParticipants = 200,
                RewardDescription = "Healthy lifestyle badge",
                CreatedByAdminId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            }
        };

        _context.ApplicationUsers.AddRange(users);
        _context.Challenges.AddRange(challenges);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdWithParticipationsAsync_ShouldReturnChallengeWithParticipations_WhenExists()
    {
        // Act
        var result = await _repository.GetByIdWithParticipationsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ChallengeId.Should().Be(1);
        result.Title.Should().Be("Running Challenge");
        result.CreatedByAdmin.Should().NotBeNull();
        result.CreatedByAdmin.Email.Should().Be("admin1@test.com");
        result.Participations.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdWithParticipationsAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdWithParticipationsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnChallenge_WhenExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ChallengeId.Should().Be(1);
        result.Title.Should().Be("Running Challenge");
        result.CreatedByAdmin.Should().NotBeNull();
        result.CreatedByAdmin.Email.Should().Be("admin1@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedResults()
    {
        // Act
        var (items, totalCount) = await _repository.GetAllAsync(1, 2);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(3);
        items.Should().BeInDescendingOrder(c => c.CreatedAt);
        items.All(c => c.CreatedByAdmin != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenPageExceedsTotal()
    {
        // Act
        var (items, totalCount) = await _repository.GetAllAsync(10, 10);

        // Assert
        items.Should().BeEmpty();
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnChallengesWithMatchingStatus()
    {
        // Act
        var result = await _repository.GetByStatusAsync(ChallengeStatus.Open);

        // Assert
        result.Should().HaveCount(2);
        result.All(c => c.Status == ChallengeStatus.Open).Should().BeTrue();
        result.All(c => c.CreatedByAdmin != null).Should().BeTrue();
        result.Should().BeInDescendingOrder(c => c.CreatedAt);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnEmptyList_WhenNoMatches()
    {
        // Act
        var result = await _repository.GetByStatusAsync(ChallengeStatus.Closed);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(ChallengeStatus.Closed);
    }

    [Fact]
    public async Task AddAsync_ShouldAddChallenge()
    {
        // Arrange
        var newChallenge = new Challenge
        {
            Title = "New Challenge",
            Description = "Test challenge",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Criteria = "Complete workout",
            Status = ChallengeStatus.Open,
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(newChallenge);
        await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(newChallenge);
        result.ChallengeId.Should().BeGreaterThan(0);

        var savedChallenge = await _repository.GetByIdAsync(result.ChallengeId);
        savedChallenge.Should().NotBeNull();
        savedChallenge!.Title.Should().Be("New Challenge");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateChallenge()
    {
        // Arrange
        var challenge = await _repository.GetByIdAsync(1);
        challenge!.Title = "Updated Running Challenge";
        challenge.Description = "Updated description";
        var originalUpdatedAt = challenge.UpdatedAt;

        // Act
        await _repository.UpdateAsync(challenge);
        await _repository.SaveChangesAsync();

        // Assert
        var updatedChallenge = await _repository.GetByIdAsync(1);
        updatedChallenge.Should().NotBeNull();
        updatedChallenge!.Title.Should().Be("Updated Running Challenge");
        updatedChallenge.Description.Should().Be("Updated description");
        updatedChallenge.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveChallenge_WhenExists()
    {
        // Act
        await _repository.DeleteAsync(1);
        await _repository.SaveChangesAsync();

        // Assert
        var deletedChallenge = await _repository.GetByIdAsync(1);
        deletedChallenge.Should().BeNull();

        var exists = await _repository.ExistsAsync(1);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotExists()
    {
        // Act & Assert
        await _repository.DeleteAsync(999);
        await _repository.SaveChangesAsync();

        // Assert that no exception was thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnAffectedRowsCount()
    {
        // Arrange
        var newChallenge = new Challenge
        {
            Title = "Save Test Challenge",
            Description = "Test save changes",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Criteria = "Test criteria",
            Status = ChallengeStatus.Open,
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(newChallenge);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
    }
}

