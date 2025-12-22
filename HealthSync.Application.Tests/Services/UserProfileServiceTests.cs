using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Features.Users.Services;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;
using FluentAssertions;

namespace HealthSync.Application.Tests.Services;

public class UserProfileServiceTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _service = new UserProfileService(_userProfileRepositoryMock.Object);
    }

    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnDto_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var profile = new UserProfile
        {
            UserProfileId = 1,
            UserId = userId,
            FullName = "John Doe",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 175,
            CurrentWeightKg = 70,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            AvatarUrl = "avatar.jpg",
            ContributionPoints = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserProfileId.Should().Be(profile.UserProfileId);
        result.UserId.Should().Be(profile.UserId);
        result.FullName.Should().Be(profile.FullName);
        result.Gender.Should().Be(profile.Gender.ToString());
        result.DateOfBirth.Should().Be(profile.DateOfBirth);
        result.HeightCm.Should().Be(profile.HeightCm);
        result.CurrentWeightKg.Should().Be(profile.CurrentWeightKg);
        result.ActivityLevel.Should().Be(profile.ActivityLevel.ToString());
        result.AvatarUrl.Should().Be(profile.AvatarUrl);
        result.ContributionPoints.Should().Be(profile.ContributionPoints);
        result.CreatedAt.Should().Be(profile.CreatedAt);
        result.UpdatedAt.Should().Be(profile.UpdatedAt);
    }

    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnNull_WhenProfileNotExists()
    {
        // Arrange
        var userId = 1;
        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ShouldUpdateAndReturnDto_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var existingProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = userId,
            FullName = "Old Name",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 170,
            CurrentWeightKg = 65,
            ActivityLevel = ActivityLevel.LightlyActive,
            AvatarUrl = "old.jpg",
            ContributionPoints = 50,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new UpdateUserProfileRequest(
            FullName: "New Name",
            Gender: "Female",
            DateOfBirth: new DateTime(1992, 5, 15),
            HeightCm: 165,
            CurrentWeightKg: 60,
            ActivityLevel: "VeryActive",
            AvatarUrl: "new.jpg"
        );

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingProfile);

        // Act
        var result = await _service.UpdateUserProfileAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be(request.FullName);
        result.Gender.Should().Be(request.Gender);
        result.DateOfBirth.Should().Be(request.DateOfBirth);
        result.HeightCm.Should().Be(request.HeightCm);
        result.CurrentWeightKg.Should().Be(request.CurrentWeightKg);
        result.ActivityLevel.Should().Be(request.ActivityLevel);
        result.AvatarUrl.Should().Be(request.AvatarUrl);
        result.UpdatedAt.Should().NotBe(default(DateTime)); // Just check it's set

        _userProfileRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.FullName == request.FullName &&
            p.Gender == Gender.Female &&
            p.DateOfBirth == request.DateOfBirth &&
            p.HeightCm == request.HeightCm &&
            p.CurrentWeightKg == request.CurrentWeightKg &&
            p.ActivityLevel == ActivityLevel.VeryActive &&
            p.AvatarUrl == request.AvatarUrl
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ShouldThrowKeyNotFoundException_WhenProfileNotExists()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserProfileRequest(
            FullName: "New Name",
            Gender: null,
            DateOfBirth: null,
            HeightCm: null,
            CurrentWeightKg: null,
            ActivityLevel: null,
            AvatarUrl: null
        );

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateUserProfileAsync(request, userId));
    }

    [Fact]
    public async Task UpdateAvatarAsync_ShouldUpdateAvatarUrl_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var avatarUrl = "new-avatar.jpg";
        var existingProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = userId,
            FullName = "John Doe",
            AvatarUrl = "old-avatar.jpg",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingProfile);

        // Act
        await _service.UpdateAvatarAsync(userId, avatarUrl);

        // Assert
        _userProfileRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.AvatarUrl == avatarUrl
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatarAsync_ShouldThrowKeyNotFoundException_WhenProfileNotExists()
    {
        // Arrange
        var userId = 1;
        var avatarUrl = "new-avatar.jpg";

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateAvatarAsync(userId, avatarUrl));
    }
}

