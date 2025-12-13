using FluentAssertions;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class ChallengeTests
{
    [Fact]
    public void Challenge_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var challenge = new Challenge();

        // Assert
        challenge.ChallengeId.Should().Be(0);
        challenge.Title.Should().BeNull();
        challenge.Description.Should().BeNull();
        challenge.ChallengeType.Should().Be(ChallengeType.Workout); // Default enum value
        challenge.StartDate.Should().Be(default(DateTime));
        challenge.EndDate.Should().Be(default(DateTime));
        challenge.Criteria.Should().BeNull();
        challenge.Status.Should().Be(ChallengeStatus.Open); // Default enum value
        challenge.MaxParticipants.Should().BeNull();
        challenge.RewardDescription.Should().BeNull();
        challenge.ImageUrl.Should().BeNull();
        challenge.CreatedByAdminId.Should().Be(0);
        challenge.CreatedAt.Should().Be(default(DateTime));
        challenge.UpdatedAt.Should().Be(default(DateTime));
        challenge.Participations.Should().NotBeNull();
        challenge.Participations.Should().BeEmpty();
    }

    [Fact]
    public void Challenge_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var challenge = new Challenge();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(30);
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddHours(1);

        // Act
        challenge.ChallengeId = 1;
        challenge.Title = "30-Day Running Challenge";
        challenge.Description = "Run 50km total";
        challenge.ChallengeType = ChallengeType.Workout;
        challenge.StartDate = startDate;
        challenge.EndDate = endDate;
        challenge.Criteria = "Upload running app screenshot";
        challenge.Status = ChallengeStatus.Open;
        challenge.MaxParticipants = 100;
        challenge.RewardDescription = "Certificate";
        challenge.ImageUrl = "https://example.com/challenge.jpg";
        challenge.CreatedByAdminId = 5;
        challenge.CreatedAt = createdAt;
        challenge.UpdatedAt = updatedAt;

        // Assert
        challenge.ChallengeId.Should().Be(1);
        challenge.Title.Should().Be("30-Day Running Challenge");
        challenge.Description.Should().Be("Run 50km total");
        challenge.ChallengeType.Should().Be(ChallengeType.Workout);
        challenge.StartDate.Should().Be(startDate);
        challenge.EndDate.Should().Be(endDate);
        challenge.Criteria.Should().Be("Upload running app screenshot");
        challenge.Status.Should().Be(ChallengeStatus.Open);
        challenge.MaxParticipants.Should().Be(100);
        challenge.RewardDescription.Should().Be("Certificate");
        challenge.ImageUrl.Should().Be("https://example.com/challenge.jpg");
        challenge.CreatedByAdminId.Should().Be(5);
        challenge.CreatedAt.Should().Be(createdAt);
        challenge.UpdatedAt.Should().Be(updatedAt);
    }
}