using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class ChallengeParticipationServiceTests
{
    private readonly Mock<IChallengeParticipationRepository> _participationRepositoryMock;
    private readonly Mock<IChallengeRepository> _challengeRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ChallengeParticipationService>> _loggerMock;
    private readonly ChallengeParticipationService _service;

    public ChallengeParticipationServiceTests()
    {
        _participationRepositoryMock = new Mock<IChallengeParticipationRepository>();
        _challengeRepositoryMock = new Mock<IChallengeRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<ChallengeParticipationService>>();

        _service = new ChallengeParticipationService(
            _participationRepositoryMock.Object,
            _challengeRepositoryMock.Object,
            _storageServiceMock.Object);
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldCreateParticipation_WhenValidRequest()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;

        var challenge = new Challenge
        {
            ChallengeId = challengeId,
            Title = "Test Challenge",
            Status = ChallengeStatus.Open,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(10),
            MaxParticipants = 100
        };

        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = challengeId,
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            Status = ParticipationStatus.Joined
        };

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync(challenge);

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync((ChallengeParticipation?)null);

        _participationRepositoryMock
            .Setup(r => r.GetParticipantCountAsync(challengeId))
            .ReturnsAsync(5);

        _participationRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChallengeParticipation>()))
            .ReturnsAsync(participation);

        _participationRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.JoinChallengeAsync(challengeId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ChallengeId.Should().Be(challengeId);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(ParticipationStatus.Joined);
        result.JoinedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _participationRepositoryMock.Verify(r => r.AddAsync(It.Is<ChallengeParticipation>(p =>
            p.ChallengeId == challengeId &&
            p.UserId == userId &&
            p.Status == ParticipationStatus.Joined)), Times.Once);
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldThrowArgumentException_WhenChallengeNotFound()
    {
        // Arrange
        var challengeId = 999;
        var userId = 1;

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync((Challenge?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.JoinChallengeAsync(challengeId, userId));
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldThrowInvalidOperationException_WhenChallengeNotOpen()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;

        var challenge = new Challenge
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Closed,
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync(challenge);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.JoinChallengeAsync(challengeId, userId));
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldThrowInvalidOperationException_WhenChallengeEnded()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;

        var challenge = new Challenge
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync(challenge);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.JoinChallengeAsync(challengeId, userId));
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldThrowInvalidOperationException_WhenAlreadyJoined()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;

        var challenge = new Challenge
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        var existingParticipation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = challengeId,
            UserId = userId,
            Status = ParticipationStatus.Joined
        };

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync(challenge);

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync(existingParticipation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.JoinChallengeAsync(challengeId, userId));
    }

    [Fact]
    public async Task JoinChallengeAsync_ShouldThrowInvalidOperationException_WhenMaxParticipantsReached()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;

        var challenge = new Challenge
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            EndDate = DateTime.UtcNow.AddDays(10),
            MaxParticipants = 10
        };

        _challengeRepositoryMock
            .Setup(r => r.GetByIdAsync(challengeId))
            .ReturnsAsync(challenge);

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync((ChallengeParticipation?)null);

        _participationRepositoryMock
            .Setup(r => r.GetParticipantCountAsync(challengeId))
            .ReturnsAsync(10);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.JoinChallengeAsync(challengeId, userId));
    }

    [Fact]
    public async Task SubmitChallengeResultAsync_ShouldUpdateToPendingApproval_WhenValidSubmission()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Completed the challenge!",
            SubmissionImage = null
        };

        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = challengeId,
            UserId = userId,
            Status = ParticipationStatus.Joined,
            JoinedDate = DateTime.UtcNow.AddDays(-5)
        };

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync(participation);

        _participationRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ChallengeParticipation>()))
            .Returns(Task.CompletedTask);

        _participationRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.SubmitChallengeResultAsync(challengeId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ParticipationStatus.PendingApproval);
        result.SubmissionText.Should().Be("Completed the challenge!");
        result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _participationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ChallengeParticipation>()), Times.Once);
    }

    [Fact]
    public async Task SubmitChallengeResultAsync_ShouldUploadImageAndSetUrl_WhenImageProvided()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;
        var mockFile = new Mock<IFormFile>();
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Check out my progress!",
            SubmissionImage = mockFile.Object
        };

        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = challengeId,
            UserId = userId,
            Status = ParticipationStatus.Joined
        };

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync(participation);

        _storageServiceMock
            .Setup(s => s.UploadAsync(mockFile.Object, It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("https://storage.example.com/submission.jpg");

        _participationRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ChallengeParticipation>()))
            .Returns(Task.CompletedTask);

        _participationRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.SubmitChallengeResultAsync(challengeId, userId, request);

        // Assert
        result.SubmissionUrl.Should().Be("https://storage.example.com/submission.jpg");

        _storageServiceMock.Verify(s => s.UploadAsync(mockFile.Object, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SubmitChallengeResultAsync_ShouldThrowArgumentException_WhenNotJoined()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync((ChallengeParticipation?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SubmitChallengeResultAsync(challengeId, userId, request));
    }

    [Fact]
    public async Task SubmitChallengeResultAsync_ShouldThrowInvalidOperationException_WhenAlreadySubmitted()
    {
        // Arrange
        var challengeId = 1;
        var userId = 1;
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = challengeId,
            UserId = userId,
            Status = ParticipationStatus.PendingApproval
        };

        _participationRepositoryMock
            .Setup(r => r.GetUserParticipationAsync(challengeId, userId))
            .ReturnsAsync(participation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitChallengeResultAsync(challengeId, userId, request));
    }
}

