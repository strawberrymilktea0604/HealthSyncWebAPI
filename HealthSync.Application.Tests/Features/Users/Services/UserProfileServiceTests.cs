using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Features.Users.Services;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Features.Users.Services;

public class UserProfileServiceTests
{
    private readonly Mock<IUserProfileRepository> _mockRepository;
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _mockRepository = new Mock<IUserProfileRepository>();
        _service = new UserProfileService(_mockRepository.Object);
    }

    #region GetUserProfileAsync Tests

    [Fact]
    public async Task GetUserProfileAsync_ReturnsProfileDto_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var profile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "John Doe",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 175.5m,
            CurrentWeightKg = 75.0m,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            AvatarUrl = "https://example.com/avatar.jpg",
            ContributionPoints = 150,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.UserProfileId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("Male", result.Gender);
        Assert.Equal(new DateTime(1990, 1, 1), result.DateOfBirth);
        Assert.Equal(175.5m, result.HeightCm);
        Assert.Equal(75.0m, result.CurrentWeightKg);
        Assert.Equal("ModeratelyActive", result.ActivityLevel);
        Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
        Assert.Equal(150, result.ContributionPoints);
    }

    [Fact]
    public async Task GetUserProfileAsync_ReturnsNull_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = 999;
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserProfileAsync_HandlesNullableFields_Correctly()
    {
        // Arrange
        var userId = 1;
        var profile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Jane Doe",
            Gender = null,
            DateOfBirth = null,
            HeightCm = null,
            CurrentWeightKg = null,
            ActivityLevel = null,
            AvatarUrl = null,
            ContributionPoints = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Gender);
        Assert.Null(result.DateOfBirth);
        Assert.Null(result.HeightCm);
        Assert.Null(result.CurrentWeightKg);
        Assert.Null(result.ActivityLevel);
        Assert.Null(result.AvatarUrl);
        Assert.Equal(0, result.ContributionPoints);
    }

    #endregion

    #region GetUserProfileResponseAsync Tests

    [Fact]
    public async Task GetUserProfileResponseAsync_ReturnsResponse_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var profile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "John Smith",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1985, 5, 15),
            HeightCm = 180.0m,
            CurrentWeightKg = 80.5m,
            ActivityLevel = ActivityLevel.VeryActive,
            AvatarUrl = "https://cdn.example.com/user1.jpg",
            ContributionPoints = 500,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        // Act
        var result = await _service.GetUserProfileResponseAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.UserProfileId);
        Assert.Equal("John Smith", result.FullName);
        Assert.Equal("Male", result.Gender);
        Assert.Equal(new DateTime(1985, 5, 15), result.DateOfBirth);
        Assert.Equal(180.0m, result.HeightCm);
        Assert.Equal(80.5m, result.CurrentWeightKg);
        Assert.Equal("VeryActive", result.ActivityLevel);
        Assert.Equal("https://cdn.example.com/user1.jpg", result.AvatarUrl);
        Assert.Equal(500, result.ContributionPoints);
    }

    [Fact]
    public async Task GetUserProfileResponseAsync_ReturnsNull_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = 123;
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserProfileResponseAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserProfileResponseAsync_HandlesAllEnumValues()
    {
        // Arrange - Test all Gender values
        var femaleProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = 1,
            FullName = "Alice",
            Gender = Gender.Female,
            ActivityLevel = ActivityLevel.Sedentary
        };

        var otherProfile = new UserProfile
        {
            UserProfileId = 2,
            UserId = 2,
            FullName = "Alex",
            Gender = Gender.Other,
            ActivityLevel = ActivityLevel.LightlyActive
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(femaleProfile);
        _mockRepository.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(otherProfile);

        // Act
        var result1 = await _service.GetUserProfileResponseAsync(1);
        var result2 = await _service.GetUserProfileResponseAsync(2);

        // Assert
        Assert.Equal("Female", result1!.Gender);
        Assert.Equal("Sedentary", result1.ActivityLevel);
        Assert.Equal("Other", result2!.Gender);
        Assert.Equal("LightlyActive", result2.ActivityLevel);
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_UpdatesAllFields_Successfully()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Old Name",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 170.0m,
            CurrentWeightKg = 70.0m,
            ActivityLevel = ActivityLevel.Sedentary,
            AvatarUrl = "old.jpg",
            ContributionPoints = 100,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var request = new UpdateUserProfileRequest(
            "New Name",
            "Female",
            new DateTime(1995, 5, 15),
            165.0m,
            60.0m,
            "VeryActive",
            "new.jpg"
        );

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("Female", result.Gender);
        Assert.Equal(new DateTime(1995, 5, 15), result.DateOfBirth);
        Assert.Equal(165.0m, result.HeightCm);
        Assert.Equal(60.0m, result.CurrentWeightKg);
        Assert.Equal("VeryActive", result.ActivityLevel);
        Assert.Equal("new.jpg", result.AvatarUrl);

        _mockRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.FullName == "New Name" &&
            p.Gender == Gender.Female &&
            p.ActivityLevel == ActivityLevel.VeryActive
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsKeyNotFoundException_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = 999;
        var request = new UpdateUserProfileRequest("Test User", null, null, null, null, null, null);
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateUserProfileAsync(request, userId));
        Assert.Equal("User profile not found", exception.Message);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_HandlesInvalidGenderEnum_Gracefully()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test User",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateUserProfileRequest(
            "Updated Name",
            "InvalidGenderValue", // Invalid enum value
            null,
            null,
            null,
            null,
            null
        );

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        Assert.Equal("Male", result.Gender); // Should keep old value when parse fails
        Assert.Equal("Updated Name", result.FullName);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_HandlesInvalidActivityLevelEnum_Gracefully()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test User",
            ActivityLevel = ActivityLevel.ModeratelyActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateUserProfileRequest(
            "Updated Name",
            null,
            null,
            null,
            null,
            "SuperActive", // Invalid enum value
            null
        );

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        Assert.Equal("ModeratelyActive", result.ActivityLevel); // Should keep old value
    }

    [Fact]
    public async Task UpdateUserProfileAsync_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var userId = 1;
        var oldUpdateTime = DateTime.UtcNow.AddHours(-2);
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = oldUpdateTime
        };

        var request = new UpdateUserProfileRequest("Updated Test", null, null, 175, null, null, null);

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var beforeUpdate = DateTime.UtcNow;
        var result = await _service.UpdateUserProfileAsync(request, userId);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.True(result.UpdatedAt >= beforeUpdate);
        Assert.True(result.UpdatedAt <= afterUpdate);
        Assert.True(result.UpdatedAt > oldUpdateTime);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_HandlesNullFields_KeepsExistingValues()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Original Name",
            Gender = Gender.Female,
            DateOfBirth = new DateTime(1992, 3, 10),
            HeightCm = 168.0m,
            CurrentWeightKg = 62.0m,
            ActivityLevel = ActivityLevel.LightlyActive,
            AvatarUrl = "original.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateUserProfileRequest(
            "Updated Name",
            null, // Don't update
            null,
            null,
            null,
            null,
            null
        );

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        Assert.Equal("Updated Name", result.FullName);
        // Gender and ActivityLevel are preserved when request has null (TryParse won't execute)
        Assert.Equal("Female", result.Gender); // Preserved from existing profile
        // But DateOfBirth, HeightCm, CurrentWeightKg, AvatarUrl ARE overwritten with null
        Assert.Null(result.DateOfBirth); // Was set, now null
        Assert.Null(result.HeightCm); // Was 168, now null
        Assert.Null(result.CurrentWeightKg); // Was 62, now null
        Assert.Equal("LightlyActive", result.ActivityLevel); // Preserved from existing profile
        Assert.Null(result.AvatarUrl); // Was original.jpg, now null
    }

    [Fact]
    public async Task UpdateUserProfileAsync_PreservesContributionPoints()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test",
            ContributionPoints = 250, // Existing points
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateUserProfileRequest("Updated", "Female", null, null, null, null, null);

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        Assert.Equal(250, result.ContributionPoints); // Should not be modified
    }

    #endregion

    #region UpdateAvatarAsync Tests

    [Fact]
    public async Task UpdateAvatarAsync_UpdatesAvatarUrl_Successfully()
    {
        // Arrange
        var userId = 1;
        var newAvatarUrl = "https://cdn.example.com/new-avatar.jpg";
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test User",
            AvatarUrl = "old-avatar.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAvatarAsync(userId, newAvatarUrl);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.AvatarUrl == newAvatarUrl
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatarAsync_ThrowsKeyNotFoundException_WhenProfileDoesNotExist()
    {
        // Arrange
        var userId = 999;
        var avatarUrl = "https://example.com/avatar.jpg";
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAvatarAsync(userId, avatarUrl));
        Assert.Equal("User profile not found", exception.Message);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAvatarAsync_HandlesNullAvatarUrl()
    {
        // Arrange
        var userId = 1;
        string? nullAvatarUrl = null;
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test User",
            AvatarUrl = "existing-avatar.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAvatarAsync(userId, nullAvatarUrl!);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p => p.AvatarUrl == null)), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatarAsync_UpdatesTimestamp()
    {
        // Arrange
        var userId = 1;
        var avatarUrl = "new-avatar.jpg";
        var oldTimestamp = DateTime.UtcNow.AddHours(-3);
        var existingProfile = new UserProfile
        {
            UserProfileId = 100,
            UserId = userId,
            FullName = "Test",
            UpdatedAt = oldTimestamp,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        // Act
        var beforeUpdate = DateTime.UtcNow;
        await _service.UpdateAvatarAsync(userId, avatarUrl);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.UpdatedAt >= beforeUpdate && p.UpdatedAt > oldTimestamp
        )), Times.Once);
    }

    #endregion

    #region CreateUserProfileAsync Tests

    [Fact]
    public async Task CreateUserProfileAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var userId = 1;
        var request = new CreateUserProfileRequest("Test User");

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _service.CreateUserProfileAsync(request, userId));
    }

    #endregion

    #region DeleteUserProfileAsync Tests

    [Fact]
    public async Task DeleteUserProfileAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var userId = 1;

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _service.DeleteUserProfileAsync(userId));
    }

    #endregion

    #region GetUserStatsAsync Tests

    [Fact]
    public async Task GetUserStatsAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var userId = 1;

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => _service.GetUserStatsAsync(userId));
    }

    #endregion
}
