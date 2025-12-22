using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class UserProfileRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserProfileRepository _repository;

    public UserProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserProfileRepository(_context);
    }

    [Fact]
    public async Task AddAsync_AddsUserProfile()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "Test User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            HeightCm = 175,
            CurrentWeightKg = 70,
            ActivityLevel = ActivityLevel.ModeratelyActive
        };

        // Act
        await _repository.AddAsync(userProfile);

        // Assert
        var savedProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.UserId);
        Assert.NotNull(savedProfile);
        Assert.Equal("Test User", savedProfile.FullName);
        Assert.Equal(175, savedProfile.HeightCm);
        Assert.Equal(70, savedProfile.CurrentWeightKg);
    }

    [Fact]
    public async Task GetByUserIdAsync_ExistingUser_ReturnsUserProfile()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "John Doe",
            DateOfBirth = new DateTime(1985, 5, 15),
            Gender = Gender.Male,
            HeightCm = 180,
            CurrentWeightKg = 75
        };
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(180, result.HeightCm);
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserProfile()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "Jane Smith",
            CurrentWeightKg = 60,
            ContributionPoints = 10
        };
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Act
        userProfile.FullName = "Jane Doe";
        userProfile.CurrentWeightKg = 58;
        userProfile.ContributionPoints = 25;
        await _repository.UpdateAsync(userProfile);

        // Assert
        var updatedProfile = await _context.UserProfiles.FindAsync(userProfile.UserProfileId);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Jane Doe", updatedProfile.FullName);
        Assert.Equal(58, updatedProfile.CurrentWeightKg);
        Assert.Equal(25, updatedProfile.ContributionPoints);
    }

    [Fact]
    public async Task SaveChangesAsync_SavesPendingChanges()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "Test User"
        };
        _context.UserProfiles.Add(userProfile);

        // Act
        await _repository.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.UserId);
        Assert.NotNull(savedProfile);
    }

    [Fact]
    public async Task GetTopUsersByContributionPointsAsync_ReturnsTopUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        for (int i = 1; i <= 5; i++)
        {
            var user = new ApplicationUser { Email = $"user{i}@test.com", PasswordHash = "hash", Role = "Customer" };
            _context.ApplicationUsers.Add(user);
            users.Add(user);
        }
        await _context.SaveChangesAsync();

        var profile1 = new UserProfile { UserId = users[0].UserId, FullName = "User1", ContributionPoints = 100 };
        var profile2 = new UserProfile { UserId = users[1].UserId, FullName = "User2", ContributionPoints = 200 };
        var profile3 = new UserProfile { UserId = users[2].UserId, FullName = "User3", ContributionPoints = 150 };
        var profile4 = new UserProfile { UserId = users[3].UserId, FullName = "User4", ContributionPoints = 50 };
        var profile5 = new UserProfile { UserId = users[4].UserId, FullName = "User5", ContributionPoints = 300 };

        _context.UserProfiles.AddRange(profile1, profile2, profile3, profile4, profile5);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTopUsersByContributionPointsAsync(3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        var topUsers = result.ToList();
        Assert.Equal(300, topUsers[0].ContributionPoints); // User5
        Assert.Equal(200, topUsers[1].ContributionPoints); // User2
        Assert.Equal(150, topUsers[2].ContributionPoints); // User3
    }

    [Fact]
    public async Task GetTopUsersByContributionPointsAsync_RequestMoreThanAvailable_ReturnsAllUsers()
    {
        // Arrange
        var user1 = new ApplicationUser { Email = "user1@test.com", PasswordHash = "hash", Role = "Customer" };
        var user2 = new ApplicationUser { Email = "user2@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var profile1 = new UserProfile { UserId = user1.UserId, FullName = "User1", ContributionPoints = 100 };
        var profile2 = new UserProfile { UserId = user2.UserId, FullName = "User2", ContributionPoints = 200 };
        _context.UserProfiles.AddRange(profile1, profile2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTopUsersByContributionPointsAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllUsersByContributionPointsAsync_ReturnsAllUsersOrderedByPoints()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        for (int i = 1; i <= 4; i++)
        {
            var user = new ApplicationUser { Email = $"user{i}@test.com", PasswordHash = "hash", Role = "Customer" };
            _context.ApplicationUsers.Add(user);
            users.Add(user);
        }
        await _context.SaveChangesAsync();

        var profile1 = new UserProfile { UserId = users[0].UserId, FullName = "User1", ContributionPoints = 80 };
        var profile2 = new UserProfile { UserId = users[1].UserId, FullName = "User2", ContributionPoints = 120 };
        var profile3 = new UserProfile { UserId = users[2].UserId, FullName = "User3", ContributionPoints = 50 };
        var profile4 = new UserProfile { UserId = users[3].UserId, FullName = "User4", ContributionPoints = 200 };

        _context.UserProfiles.AddRange(profile1, profile2, profile3, profile4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllUsersByContributionPointsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count());

        var allUsers = result.ToList();
        Assert.Equal(200, allUsers[0].ContributionPoints); // User4
        Assert.Equal(120, allUsers[1].ContributionPoints); // User2
        Assert.Equal(80, allUsers[2].ContributionPoints);  // User1
        Assert.Equal(50, allUsers[3].ContributionPoints);  // User3
    }

    [Fact]
    public async Task GetAllUsersByContributionPointsAsync_NoUsers_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllUsersByContributionPointsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_WithAllFields_SavesCorrectly()
    {
        // Arrange
        var user = new ApplicationUser { Email = "complete@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "Complete User",
            DateOfBirth = new DateTime(1995, 3, 20),
            Gender = Gender.Female,
            HeightCm = 165,
            CurrentWeightKg = 55,
            ActivityLevel = ActivityLevel.VeryActive,
            AvatarUrl = "https://example.com/avatar.jpg",
            ContributionPoints = 50
        };

        // Act
        await _repository.AddAsync(userProfile);

        // Assert
        var savedProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.UserId);
        Assert.NotNull(savedProfile);
        Assert.Equal("Complete User", savedProfile.FullName);
        Assert.Equal(new DateTime(1995, 3, 20), savedProfile.DateOfBirth);
        Assert.Equal(Gender.Female, savedProfile.Gender);
        Assert.Equal(165, savedProfile.HeightCm);
        Assert.Equal(55, savedProfile.CurrentWeightKg);
        Assert.Equal(ActivityLevel.VeryActive, savedProfile.ActivityLevel);
        Assert.Equal("https://example.com/avatar.jpg", savedProfile.AvatarUrl);
        Assert.Equal(50, savedProfile.ContributionPoints);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = "Test User",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var originalTimestamp = userProfile.UpdatedAt;

        // Act
        userProfile.FullName = "Updated User";
        userProfile.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(userProfile);

        // Assert
        var updatedProfile = await _context.UserProfiles.FindAsync(userProfile.UserProfileId);
        Assert.NotNull(updatedProfile);
        Assert.True(updatedProfile.UpdatedAt > originalTimestamp);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


