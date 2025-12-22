using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ChallengeParticipationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ChallengeParticipationRepository _repository;

    public ChallengeParticipationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ChallengeParticipationRepository(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { UserId = 1, Email = "user1@test.com", PasswordHash = "hash1", Role = "Customer", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ApplicationUser { UserId = 2, Email = "user2@test.com", PasswordHash = "hash2", Role = "Customer", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ApplicationUser { UserId = 3, Email = "admin@test.com", PasswordHash = "hash3", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var userProfiles = new List<UserProfile>
        {
            new UserProfile { UserProfileId = 1, UserId = 1, FullName = "User One", DateOfBirth = new DateTime(1990, 1, 1), Gender = Gender.Male, HeightCm = 175, CurrentWeightKg = 70, ActivityLevel = ActivityLevel.ModeratelyActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new UserProfile { UserProfileId = 2, UserId = 2, FullName = "User Two", DateOfBirth = new DateTime(1992, 2, 2), Gender = Gender.Female, HeightCm = 165, CurrentWeightKg = 60, ActivityLevel = ActivityLevel.LightlyActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new UserProfile { UserProfileId = 3, UserId = 3, FullName = "Admin User", DateOfBirth = new DateTime(1985, 3, 3), Gender = Gender.Male, HeightCm = 180, CurrentWeightKg = 80, ActivityLevel = ActivityLevel.VeryActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var challenges = new List<Challenge>
        {
            new Challenge
            {
                ChallengeId = 1,
                Title = "Running Challenge",
                Description = "Run 50km",
                ChallengeType = ChallengeType.Workout,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(10),
                Criteria = "Run 50km total in 30 days",
                Status = ChallengeStatus.Open,
                CreatedByAdminId = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Challenge
            {
                ChallengeId = 2,
                Title = "Walking Challenge",
                Description = "Walk 100km",
                ChallengeType = ChallengeType.Workout,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                Criteria = "Walk 100km total in 30 days",
                Status = ChallengeStatus.Open,
                CreatedByAdminId = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                JoinedDate = DateTime.UtcNow.AddDays(-5),
                Status = ParticipationStatus.Joined,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new ChallengeParticipation
            {
                ParticipationId = 2,
                ChallengeId = 1,
                UserId = 2,
                JoinedDate = DateTime.UtcNow.AddDays(-3),
                Status = ParticipationStatus.PendingApproval,
                SubmissionText = "Completed the challenge",
                SubmissionUrl = "https://example.com/proof.jpg",
                SubmittedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new ChallengeParticipation
            {
                ParticipationId = 3,
                ChallengeId = 2,
                UserId = 1,
                JoinedDate = DateTime.UtcNow.AddDays(-2),
                Status = ParticipationStatus.Completed,
                SubmissionText = "Finished walking",
                SubmittedAt = DateTime.UtcNow.AddDays(-1),
                ReviewedByAdminId = 3,
                ReviewDate = DateTime.UtcNow,
                ReviewNotes = "Great job!",
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ChallengeParticipation
            {
                ParticipationId = 4,
                ChallengeId = 1,
                UserId = 1,
                JoinedDate = DateTime.UtcNow.AddDays(-1),
                Status = ParticipationStatus.Failed,
                SubmissionText = "Could not complete",
                SubmittedAt = DateTime.UtcNow,
                ReviewedByAdminId = 3,
                ReviewDate = DateTime.UtcNow,
                ReviewNotes = "Better luck next time",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.ApplicationUsers.AddRange(users);
        _context.UserProfiles.AddRange(userProfiles);
        _context.Challenges.AddRange(challenges);
        _context.ChallengeParticipations.AddRange(participations);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ShouldReturnParticipationWithDetails_WhenExists()
    {
        // Act
        var result = await _repository.GetByIdWithDetailsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ParticipationId.Should().Be(1);
        result.Challenge.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.ReviewedByAdmin.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdWithDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnParticipation_WhenExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ParticipationId.Should().Be(1);
        result.ChallengeId.Should().Be(1);
        result.UserId.Should().Be(1);
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
    public async Task GetByChallengeIdAsync_ShouldReturnParticipationsOrderedByJoinedDate()
    {
        // Act
        var result = await _repository.GetByChallengeIdAsync(1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(p => p.JoinedDate);
        result.All(p => p.ChallengeId == 1).Should().BeTrue();
        result.All(p => p.User != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByChallengeAndStatusAsync_ShouldReturnFilteredParticipations()
    {
        // Act
        var result = await _repository.GetByChallengeAndStatusAsync(1, ParticipationStatus.PendingApproval);

        // Assert
        result.Should().HaveCount(1);
        result[0].ParticipationId.Should().Be(2);
        result[0].Status.Should().Be(ParticipationStatus.PendingApproval);
        result[0].User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserParticipationAsync_ShouldReturnParticipation_WhenExists()
    {
        // Act
        var result = await _repository.GetUserParticipationAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.ParticipationId.Should().Be(1);
        result.ChallengeId.Should().Be(1);
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUserParticipationAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetUserParticipationAsync(1, 999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsUserParticipatedAsync_ShouldReturnTrue_WhenParticipated()
    {
        // Act
        var result = await _repository.IsUserParticipatedAsync(1, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserParticipatedAsync_ShouldReturnFalse_WhenNotParticipated()
    {
        // Act
        var result = await _repository.IsUserParticipatedAsync(2, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetParticipantCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.GetParticipantCountAsync(1);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetPendingApprovalsCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.GetPendingApprovalsCountAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetAllPendingApprovalsAsync_ShouldReturnPaginatedResults()
    {
        // Act
        var (items, totalCount) = await _repository.GetAllPendingApprovalsAsync(1, 10);

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].ParticipationId.Should().Be(2);
        items[0].Challenge.Should().NotBeNull();
        items[0].User.Should().NotBeNull();
        items[0].User.UserProfile.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddParticipation()
    {
        // Arrange
        var newParticipation = new ChallengeParticipation
        {
            ChallengeId = 2,
            UserId = 2,
            JoinedDate = DateTime.UtcNow,
            Status = ParticipationStatus.Joined,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(newParticipation);
        await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(newParticipation);
        var saved = await _repository.GetByIdAsync(result.ParticipationId);
        saved.Should().NotBeNull();
        saved!.ChallengeId.Should().Be(2);
        saved.UserId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateParticipation()
    {
        // Arrange
        var participation = await _repository.GetByIdAsync(1);
        participation!.Status = ParticipationStatus.Completed;
        participation.CompletedAt = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(participation);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(1);
        updated!.Status.Should().Be(ParticipationStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveParticipation_WhenExists()
    {
        // Act
        await _repository.DeleteAsync(1);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(1);
        deleted.Should().BeNull();
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
    public async Task GetAllAsync_ShouldReturnAllParticipations()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserParticipationsOrderedByJoinedDate()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(p => p.JoinedDate);
        result.All(p => p.UserId == 1).Should().BeTrue();
        result.All(p => p.Challenge != null).Should().BeTrue();
    }

    [Fact]
    public async Task CountCompletedByUserIdAndMonthAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        // Act
        var result = await _repository.CountCompletedByUserIdAndMonthAsync(1, year, month);

        // Assert
        result.Should().Be(1); // ParticipationId 3 is completed in current month
    }

    [Fact]
    public async Task CountCompletedByUserIdAndMonthAsync_ShouldReturnZero_WhenNoCompletedInMonth()
    {
        // Act
        var result = await _repository.CountCompletedByUserIdAndMonthAsync(2, 2023, 1);

        // Assert
        result.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

