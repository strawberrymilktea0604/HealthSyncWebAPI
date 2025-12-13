using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class ChallengeAdminServiceTests
{
    private readonly Mock<IChallengeRepository> _challengeRepositoryMock;
    private readonly Mock<IChallengeParticipationRepository> _participationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ChallengeAdminService _service;

    public ChallengeAdminServiceTests()
    {
        _challengeRepositoryMock = new Mock<IChallengeRepository>();
        _participationRepositoryMock = new Mock<IChallengeParticipationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _service = new ChallengeAdminService(
            _challengeRepositoryMock.Object,
            _participationRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateChallengeAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "30-Day Running Challenge",
            Description = "Run 50km in 30 days",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Criteria = "Upload running app screenshot",
            MaxParticipants = 100,
            RewardDescription = "Certificate"
        };

        var admin = new ApplicationUser { UserId = 1, Role = "Admin" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(admin);

        var createdChallenge = new Challenge
        {
            ChallengeId = 1,
            Title = request.Title,
            Description = request.Description,
            ChallengeType = request.ChallengeType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Criteria = request.Criteria,
            Status = ChallengeStatus.Open,
            MaxParticipants = request.MaxParticipants,
            RewardDescription = request.RewardDescription,
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _challengeRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Challenge>())).ReturnsAsync(createdChallenge);
        _challengeRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.CreateChallengeAsync(request, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(request.Title);
        result.Data.Status.Should().Be(ChallengeStatus.Open);
        result.Message.Should().Be("Challenge created successfully");
    }

    [Fact]
    public async Task CreateChallengeAsync_EndDateBeforeStartDate_ReturnsFailure()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "Invalid Challenge",
            Description = "Test",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(5), // Before start date
            Criteria = "Test"
        };

        // Act
        var result = await _service.CreateChallengeAsync(request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("End date must be after start date");
    }

    [Fact]
    public async Task CreateChallengeAsync_StartDateInPast_ReturnsFailure()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "Invalid Challenge",
            Description = "Test",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(-1), // In past
            EndDate = DateTime.UtcNow.AddDays(10),
            Criteria = "Test"
        };

        // Act
        var result = await _service.CreateChallengeAsync(request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Start date cannot be in the past");
    }

    [Fact]
    public async Task CreateChallengeAsync_AdminNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "Test Challenge",
            Description = "Test",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(10),
            Criteria = "Test"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.CreateChallengeAsync(request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Admin not found");
    }

    [Fact]
    public async Task GetChallengeAsync_ExistingChallenge_ReturnsSuccess()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 1,
            Title = "Test Challenge",
            Description = "Test",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(10),
            Criteria = "Test",
            Status = ChallengeStatus.Open,
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _challengeRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(challenge);
        _participationRepositoryMock.Setup(x => x.GetParticipantCountAsync(1)).ReturnsAsync(5);

        // Act
        var result = await _service.GetChallengeAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ChallengeId.Should().Be(1);
        result.Data.CurrentParticipants.Should().Be(5);
        result.Message.Should().Be("Challenge retrieved successfully");
    }

    [Fact]
    public async Task GetChallengeAsync_NonExistingChallenge_ReturnsFailure()
    {
        // Arrange
        _challengeRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Challenge?)null);

        // Act
        var result = await _service.GetChallengeAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Challenge not found");
    }

    [Fact]
    public async Task GetAllChallengesAsync_ReturnsPaginatedResult()
    {
        // Arrange
        var challenges = new List<Challenge>
        {
            new Challenge { ChallengeId = 1, Title = "Challenge 1", Status = ChallengeStatus.Open },
            new Challenge { ChallengeId = 2, Title = "Challenge 2", Status = ChallengeStatus.Closed }
        };

        _challengeRepositoryMock.Setup(x => x.GetAllAsync(1, 20)).ReturnsAsync((challenges, 2));
        _participationRepositoryMock.Setup(x => x.GetParticipantCountAsync(1)).ReturnsAsync(10);
        _participationRepositoryMock.Setup(x => x.GetParticipantCountAsync(2)).ReturnsAsync(5);

        // Act
        var result = await _service.GetAllChallengesAsync(1, 20);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalItems.Should().Be(2);
        result.Data.CurrentPage.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
        result.Message.Should().Be("Challenges retrieved successfully");
    }

    [Fact]
    public async Task UpdateChallengeAsync_ExistingChallenge_ReturnsSuccess()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 1,
            Title = "Original Title",
            Description = "Original Description",
            Status = ChallengeStatus.Open,
            MaxParticipants = 50,
            RewardDescription = "Original Reward",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new UpdateChallengeRequest
        {
            Title = "Updated Title",
            Status = ChallengeStatus.Closed,
            MaxParticipants = 100
        };

        _challengeRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(challenge);
        _challengeRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Challenge>())).Returns(Task.CompletedTask);
        _challengeRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _participationRepositoryMock.Setup(x => x.GetParticipantCountAsync(1)).ReturnsAsync(10);

        // Act
        var result = await _service.UpdateChallengeAsync(1, request, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Updated Title");
        result.Data.Status.Should().Be(ChallengeStatus.Closed);
        result.Data.MaxParticipants.Should().Be(100);
        result.Data.CurrentParticipants.Should().Be(10);
        result.Message.Should().Be("Challenge updated successfully");
    }

    [Fact]
    public async Task UpdateChallengeAsync_NonExistingChallenge_ReturnsFailure()
    {
        // Arrange
        var request = new UpdateChallengeRequest { Title = "Updated Title" };
        _challengeRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Challenge?)null);

        // Act
        var result = await _service.UpdateChallengeAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Challenge not found");
    }

    [Fact]
    public async Task DeleteChallengeAsync_ExistingChallenge_ReturnsSuccess()
    {
        // Arrange
        _challengeRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        _challengeRepositoryMock.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);
        _challengeRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteChallengeAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Challenge deleted successfully");
    }

    [Fact]
    public async Task DeleteChallengeAsync_NonExistingChallenge_ReturnsFailure()
    {
        // Arrange
        _challengeRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteChallengeAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Challenge not found");
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsPendingParticipations()
    {
        // Arrange
        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.PendingApproval,
                User = new ApplicationUser
                {
                    UserProfile = new UserProfile { FullName = "John Doe" }
                }
            }
        };

        _participationRepositoryMock.Setup(x => x.GetByChallengeAndStatusAsync(1, ParticipationStatus.PendingApproval))
            .ReturnsAsync(participations);

        // Act
        var result = await _service.GetPendingApprovalsAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data![0].ParticipationId.Should().Be(1);
        result.Data[0].UserFullName.Should().Be("John Doe");
        result.Message.Should().Be("Pending approvals retrieved successfully");
    }

    [Fact]
    public async Task ReviewParticipationAsync_ApprovePendingParticipation_ReturnsSuccess()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.PendingApproval,
            User = new ApplicationUser
            {
                UserProfile = new UserProfile { FullName = "John Doe" }
            }
        };

        var request = new ReviewParticipationRequest
        {
            Approved = true,
            ReviewNotes = "Great job!"
        };

        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);
        _participationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>())).Returns(Task.CompletedTask);
        _participationRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.ReviewParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(ParticipationStatus.Completed);
        result.Data.ReviewedByAdminId.Should().Be(1);
        result.Data.ReviewNotes.Should().Be("Great job!");
        result.Data.CompletedAt.Should().NotBeNull();
        result.Message.Should().Be("Participation approved successfully");
    }

    [Fact]
    public async Task ReviewParticipationAsync_RejectPendingParticipation_ReturnsSuccess()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.PendingApproval,
            User = new ApplicationUser
            {
                UserProfile = new UserProfile { FullName = "John Doe" }
            }
        };

        var request = new ReviewParticipationRequest
        {
            Approved = false,
            ReviewNotes = "Not enough evidence"
        };

        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);
        _participationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>())).Returns(Task.CompletedTask);
        _participationRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.ReviewParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(ParticipationStatus.Failed);
        result.Data.ReviewedByAdminId.Should().Be(1);
        result.Data.ReviewNotes.Should().Be("Not enough evidence");
        result.Data.CompletedAt.Should().BeNull();
        result.Message.Should().Be("Participation rejected successfully");
    }

    [Fact]
    public async Task ReviewParticipationAsync_NonExistingParticipation_ReturnsFailure()
    {
        // Arrange
        var request = new ReviewParticipationRequest { Approved = true };
        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync((ChallengeParticipation?)null);

        // Act
        var result = await _service.ReviewParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Participation not found");
    }

    [Fact]
    public async Task ReviewParticipationAsync_WrongStatus_ReturnsFailure()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            Status = ParticipationStatus.Joined // Not pending approval
        };

        var request = new ReviewParticipationRequest { Approved = true };
        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);

        // Act
        var result = await _service.ReviewParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Participation is not pending approval");
    }

    [Fact]
    public async Task GetChallengeParticipantsAsync_ReturnsAllParticipants()
    {
        // Arrange
        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.Completed,
                User = new ApplicationUser
                {
                    UserProfile = new UserProfile { FullName = "John Doe" }
                }
            },
            new ChallengeParticipation
            {
                ParticipationId = 2,
                ChallengeId = 1,
                UserId = 2,
                Status = ParticipationStatus.Joined,
                User = new ApplicationUser
                {
                    UserProfile = new UserProfile { FullName = "Jane Smith" }
                }
            }
        };

        _participationRepositoryMock.Setup(x => x.GetByChallengeIdAsync(1)).ReturnsAsync(participations);

        // Act
        var result = await _service.GetChallengeParticipantsAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data![0].UserFullName.Should().Be("John Doe");
        result.Data[1].UserFullName.Should().Be("Jane Smith");
        result.Message.Should().Be("Participants retrieved successfully");
    }

    [Fact]
    public async Task GetAllPendingApprovalsAsync_ReturnsPaginatedPendingApprovals()
    {
        // Arrange
        var participations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.PendingApproval,
                User = new ApplicationUser
                {
                    UserProfile = new UserProfile { FullName = "John Doe" }
                }
            }
        };

        _participationRepositoryMock.Setup(x => x.GetAllPendingApprovalsAsync(1, 20)).ReturnsAsync((participations, 1));

        // Act
        var result = await _service.GetAllPendingApprovalsAsync(1, 20);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalItems.Should().Be(1);
        result.Data.CurrentPage.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
        result.Message.Should().Be("Pending approvals retrieved successfully");
    }

    [Fact]
    public async Task RejectParticipationAsync_ValidPendingParticipation_ReturnsSuccess()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.PendingApproval,
            User = new ApplicationUser
            {
                UserProfile = new UserProfile { FullName = "John Doe" }
            }
        };

        var admin = new ApplicationUser { UserId = 1, Role = "Admin" };
        var request = new ReviewParticipationRequest
        {
            Approved = false,
            ReviewNotes = "Insufficient evidence"
        };

        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(admin);
        _participationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>())).Returns(Task.CompletedTask);
        _participationRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.RejectParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(ParticipationStatus.Failed);
        result.Data.ReviewedByAdminId.Should().Be(1);
        result.Data.ReviewNotes.Should().Be("Insufficient evidence");
        result.Message.Should().Be("Participation rejected successfully");
    }

    [Fact]
    public async Task RejectParticipationAsync_NonExistingParticipation_ReturnsFailure()
    {
        // Arrange
        var request = new ReviewParticipationRequest { Approved = false };
        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync((ChallengeParticipation?)null);

        // Act
        var result = await _service.RejectParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Participation not found");
    }

    [Fact]
    public async Task RejectParticipationAsync_WrongStatus_ReturnsFailure()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            Status = ParticipationStatus.Completed // Not pending approval
        };

        var request = new ReviewParticipationRequest { Approved = false };
        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);

        // Act
        var result = await _service.RejectParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Cannot reject participation with status 'Completed'. Only 'PendingApproval' status can be rejected.");
    }

    [Fact]
    public async Task RejectParticipationAsync_AdminNotFound_ReturnsFailure()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            Status = ParticipationStatus.PendingApproval
        };

        var request = new ReviewParticipationRequest { Approved = false };
        _participationRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(participation);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.RejectParticipationAsync(1, request, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Admin not found");
    }
}