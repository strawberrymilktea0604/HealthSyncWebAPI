using FluentAssertions;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var profile = new UserProfile();

        // Assert
        profile.UserProfileId.Should().Be(0);
        profile.UserId.Should().Be(0);
        profile.FullName.Should().BeNull();
        profile.DateOfBirth.Should().BeNull();
        profile.Gender.Should().BeNull();
        profile.HeightCm.Should().BeNull();
        profile.CurrentWeightKg.Should().BeNull();
        profile.ActivityLevel.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
        profile.ContributionPoints.Should().Be(0);
        profile.CreatedAt.Should().Be(default(DateTime));
        profile.UpdatedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void UserProfile_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var profile = new UserProfile();
        var dateOfBirth = new DateTime(1990, 1, 1);
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddHours(1);

        // Act
        profile.UserProfileId = 1;
        profile.UserId = 123;
        profile.FullName = "John Doe";
        profile.DateOfBirth = dateOfBirth;
        profile.Gender = Gender.Male;
        profile.HeightCm = 175.5m;
        profile.CurrentWeightKg = 70.0m;
        profile.ActivityLevel = ActivityLevel.ModeratelyActive;
        profile.AvatarUrl = "https://example.com/avatar.jpg";
        profile.ContributionPoints = 100;
        profile.CreatedAt = createdAt;
        profile.UpdatedAt = updatedAt;

        // Assert
        profile.UserProfileId.Should().Be(1);
        profile.UserId.Should().Be(123);
        profile.FullName.Should().Be("John Doe");
        profile.DateOfBirth.Should().Be(dateOfBirth);
        profile.Gender.Should().Be(Gender.Male);
        profile.HeightCm.Should().Be(175.5m);
        profile.CurrentWeightKg.Should().Be(70.0m);
        profile.ActivityLevel.Should().Be(ActivityLevel.ModeratelyActive);
        profile.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        profile.ContributionPoints.Should().Be(100);
        profile.CreatedAt.Should().Be(createdAt);
        profile.UpdatedAt.Should().Be(updatedAt);
    }
}