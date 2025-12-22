using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;

namespace HealthSync.WebApi.Tests.Controllers;

public class CommunityChallengeControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CommunityChallengeController _controller;
    private readonly Mock<IChallengeRepository> _mockChallengeRepository;
    private readonly Mock<IChallengeParticipationRepository> _mockParticipationRepository;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<HealthSync.Application.Interfaces.INotificationRepository> _mockNotificationRepository;

    public CommunityChallengeControllerTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        // Setup mocks
        _mockChallengeRepository = new Mock<IChallengeRepository>();
        _mockParticipationRepository = new Mock<IChallengeParticipationRepository>();
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockNotificationRepository = new Mock<HealthSync.Application.Interfaces.INotificationRepository>();

        // Seed test data
        SeedTestData();

        // Create controller
        _controller = new CommunityChallengeController(
            _mockChallengeRepository.Object,
            _mockParticipationRepository.Object,
            _mockFileStorage.Object,
            _mockNotificationRepository.Object);

        // Setup authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void SeedTestData()
    {
        // Seed test data for challenges and participations
        var challenge1 = new Challenge
        {
            ChallengeId = 1,
            Title = "30-Day Running Challenge",
            Description = "Run 50km in 30 days",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25),
            Criteria = "Upload running app screenshots",
            Status = ChallengeStatus.Open,
            MaxParticipants = 100,
            RewardDescription = "Certificate",
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var challenge2 = new Challenge
        {
            ChallengeId = 2,
            Title = "Nutrition Tracking Challenge",
            Description = "Track meals for 14 days",
            ChallengeType = ChallengeType.Nutrition,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(12),
            Criteria = "Log all meals daily",
            Status = ChallengeStatus.Open,
            MaxParticipants = null,
            RewardDescription = "Healthy eating badge",
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var challenge3 = new Challenge
        {
            ChallengeId = 3,
            Title = "Closed Challenge",
            Description = "This challenge is closed",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-1),
            Criteria = "Already ended",
            Status = ChallengeStatus.Closed,
            MaxParticipants = 50,
            RewardDescription = "Old reward",
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Challenges.AddRange(challenge1, challenge2, challenge3);

        // Seed participations
        var participation1 = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            JoinedDate = DateTime.UtcNow.AddDays(-3),
            Status = ParticipationStatus.Joined,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var participation2 = new ChallengeParticipation
        {
            ParticipationId = 2,
            ChallengeId = 2,
            UserId = 1,
            JoinedDate = DateTime.UtcNow.AddDays(-1),
            Status = ParticipationStatus.PendingApproval,
            SubmissionText = "Completed nutrition tracking",
            SubmissionUrl = "https://example.com/submission.jpg",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _db.ChallengeParticipations.AddRange(participation1, participation2);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    #region GetOpenChallenges Tests

    [Fact]
    public async Task GetOpenChallenges_ShouldReturnOpenChallengesSuccessfully()
    {
        // Arrange
        var challenges = new List<Challenge>
        {
            new Challenge
            {
                ChallengeId = 1,
                Title = "30-Day Running Challenge",
                Description = "Run 50km in 30 days",
                ChallengeType = ChallengeType.Workout,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(25),
                Criteria = "Upload running app screenshots",
                Status = ChallengeStatus.Open,
                MaxParticipants = 100,
                RewardDescription = "Certificate",
                CreatedByAdminId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Participations = new List<ChallengeParticipation>()
            },
            new Challenge
            {
                ChallengeId = 2,
                Title = "Nutrition Tracking Challenge",
                Description = "Track meals for 14 days",
                ChallengeType = ChallengeType.Nutrition,
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddDays(12),
                Criteria = "Log all meals daily",
                Status = ChallengeStatus.Open,
                MaxParticipants = null,
                RewardDescription = "Healthy eating badge",
                CreatedByAdminId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Participations = new List<ChallengeParticipation>()
            }
        };

        _mockChallengeRepository.Setup(x => x.GetAllAsync(1, 20))
            .ReturnsAsync((challenges, 2));

        // Act
        var result = await _controller.GetOpenChallenges(1, 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value) as IEnumerable<object>;
        var pagination = value.GetType().GetProperty("pagination")?.GetValue(value);

        Assert.True(success is true);
        Assert.NotNull(data);
        Assert.Equal(2, data.Count());
        Assert.NotNull(pagination);
    }

    [Fact]
    public async Task GetOpenChallenges_ShouldFilterOnlyOpenChallenges()
    {
        // Arrange
        var allChallenges = new List<Challenge>
        {
            new Challenge
            {
                ChallengeId = 1,
                Title = "Open Challenge",
                Status = ChallengeStatus.Open,
                Participations = new List<ChallengeParticipation>()
            },
            new Challenge
            {
                ChallengeId = 2,
                Title = "Closed Challenge",
                Status = ChallengeStatus.Closed,
                Participations = new List<ChallengeParticipation>()
            }
        };

        _mockChallengeRepository.Setup(x => x.GetAllAsync(1, 20))
            .ReturnsAsync((allChallenges, 2));

        // Act
        var result = await _controller.GetOpenChallenges(1, 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        Assert.NotNull(value);
        var dataProperty = value.GetType().GetProperty("data");
        Assert.NotNull(dataProperty);
        var rawData = dataProperty.GetValue(value);
        Assert.NotNull(rawData);
#pragma warning disable CS8602 // Dereference of a possibly null reference
        IEnumerable<object>? data = null;
        if (rawData != null)
        {
            data = rawData as IEnumerable<object>;
        }
#pragma warning restore CS8602
        Assert.NotNull(data);

        Assert.NotNull(data);
        if (data != null)
        {
            Assert.Single(data); // Only the open challenge should be returned
        }
    }

    [Fact]
    public async Task GetOpenChallenges_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockChallengeRepository.Setup(x => x.GetAllAsync(1, 20))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetOpenChallenges(1, 20);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.Equal("Database error", error);
    }

    #endregion

    #region GetMyChallenges Tests

    [Fact]
    public async Task GetMyChallenges_ShouldReturnUserParticipationsSuccessfully()
    {
        // Arrange
        var userParticipations = new List<ChallengeParticipation>
        {
            new ChallengeParticipation
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                JoinedDate = DateTime.UtcNow.AddDays(-3),
                Status = ParticipationStatus.Joined,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Challenge = new Challenge
                {
                    ChallengeId = 1,
                    Title = "30-Day Running Challenge",
                    Description = "Run 50km in 30 days",
                    ChallengeType = ChallengeType.Workout,
                    StartDate = DateTime.UtcNow.AddDays(-5),
                    EndDate = DateTime.UtcNow.AddDays(25),
                    Criteria = "Upload running app screenshots",
                    Status = ChallengeStatus.Open,
                    MaxParticipants = 100,
                    RewardDescription = "Certificate",
                    CreatedByAdminId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        _mockParticipationRepository.Setup(x => x.GetByUserIdAsync(1))
            .ReturnsAsync(userParticipations);

        // Act
        var result = await _controller.GetMyChallenges();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value) as IEnumerable<object>;

        Assert.True(success is true);
        Assert.NotNull(data);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetMyChallenges_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetMyChallenges();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult.StatusCode.Should().Be(401);

        var value = unauthorizedResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid user", message);
    }

    [Fact]
    public async Task GetMyChallenges_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockParticipationRepository.Setup(x => x.GetByUserIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMyChallenges();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.Equal("Database error", error);
    }

    #endregion

    #region Join Tests

    [Fact]
    public async Task Join_ShouldJoinChallengeSuccessfully()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 3,
            Title = "New Challenge",
            Status = ChallengeStatus.Open,
            MaxParticipants = 10,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(10),
            Participations = new List<ChallengeParticipation>()
        };

        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(3))
            .ReturnsAsync(challenge);
        _mockParticipationRepository.Setup(x => x.IsUserParticipatedAsync(3, 1))
            .ReturnsAsync(false);
        _mockParticipationRepository.Setup(x => x.GetParticipantCountAsync(3))
            .ReturnsAsync(5);
        _mockParticipationRepository.Setup(x => x.AddAsync(It.IsAny<ChallengeParticipation>()))
            .ReturnsAsync(new ChallengeParticipation { ParticipationId = 1, ChallengeId = 3, UserId = 1, Status = ParticipationStatus.Joined });
        _mockParticipationRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Join(3);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Joined challenge successfully", message);
        Assert.NotNull(data);

        _mockParticipationRepository.Verify(x => x.AddAsync(It.IsAny<ChallengeParticipation>()), Times.Once);
        _mockParticipationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Join_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.Join(1);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult.StatusCode.Should().Be(401);

        var value = unauthorizedResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid user", message);
    }

    [Fact]
    public async Task Join_ShouldReturnNotFound_WhenChallengeDoesNotExist()
    {
        // Arrange
        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(999))
            .ReturnsAsync(default(Challenge));

        // Act
        var result = await _controller.Join(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge not found", message);
    }

    [Fact]
    public async Task Join_ShouldReturnBadRequest_WhenChallengeIsNotOpen()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 3,
            Title = "Closed Challenge",
            Status = ChallengeStatus.Closed,
            Participations = new List<ChallengeParticipation>()
        };

        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(3))
            .ReturnsAsync(challenge);

        // Act
        var result = await _controller.Join(3);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge is not open", message);
    }

    [Fact]
    public async Task Join_ShouldReturnBadRequest_WhenAlreadyJoined()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 1,
            Title = "Already Joined Challenge",
            Status = ChallengeStatus.Open,
            Participations = new List<ChallengeParticipation>()
        };

        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(1))
            .ReturnsAsync(challenge);
        _mockParticipationRepository.Setup(x => x.IsUserParticipatedAsync(1, 1))
            .ReturnsAsync(true); // User already participated

        // Act
        var result = await _controller.Join(1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Already joined this challenge", message);
    }

    [Fact]
    public async Task Join_ShouldReturnBadRequest_WhenChallengeIsFull()
    {
        // Arrange
        var challenge = new Challenge
        {
            ChallengeId = 3,
            Title = "Full Challenge",
            Status = ChallengeStatus.Open,
            MaxParticipants = 10,
            Participations = new List<ChallengeParticipation>()
        };

        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(3))
            .ReturnsAsync(challenge);
        _mockParticipationRepository.Setup(x => x.IsUserParticipatedAsync(3, 1))
            .ReturnsAsync(false);
        _mockParticipationRepository.Setup(x => x.GetParticipantCountAsync(3))
            .ReturnsAsync(10); // At max capacity

        // Act
        var result = await _controller.Join(3);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge is full", message);
    }

    [Fact]
    public async Task Join_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockChallengeRepository.Setup(x => x.GetByIdWithParticipationsAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Join(1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.Equal("Database error", error);
    }

    #endregion

    #region Submit Tests

    [Fact]
    public async Task Submit_ShouldSubmitChallengeSuccessfully()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Joined,
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Test Challenge",
                Status = ChallengeStatus.Open,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5)
            }
        };

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Completed the challenge!",
            SubmissionImage = null // No image for this test
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);
        _mockParticipationRepository.Setup(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>()))
            .Returns(Task.CompletedTask);
        _mockParticipationRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockNotificationRepository.Setup(x => x.AddAsync(It.IsAny<HealthSync.Domain.Entities.Notification>()))
            .ReturnsAsync(new Notification { NotificationId = 1, Message = "Test notification", RecipientRole = "Admin" });
        _mockNotificationRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Submission received; pending admin approval", message);
        Assert.NotNull(data);

        _mockParticipationRepository.Verify(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>()), Times.Once);
        _mockParticipationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockNotificationRepository.Verify(x => x.AddAsync(It.IsAny<HealthSync.Domain.Entities.Notification>()), Times.Once);
        _mockNotificationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Submit_ShouldSubmitWithImageSuccessfully()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Joined,
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Test Challenge",
                Status = ChallengeStatus.Open,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5)
            }
        };

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Completed with proof!",
            SubmissionImage = mockFile.Object
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);
        _mockFileStorage.Setup(x => x.UploadAsync(mockFile.Object, "challenge-submissions"))
            .ReturnsAsync("https://storage.example.com/submission.jpg");
        _mockParticipationRepository.Setup(x => x.UpdateAsync(It.IsAny<ChallengeParticipation>()))
            .Returns(Task.CompletedTask);
        _mockParticipationRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockNotificationRepository.Setup(x => x.AddAsync(It.IsAny<HealthSync.Domain.Entities.Notification>()))
            .ReturnsAsync(new Notification { NotificationId = 2, Message = "Test notification 2", RecipientRole = "Admin" });
        _mockNotificationRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        _mockFileStorage.Verify(x => x.UploadAsync(mockFile.Object, "challenge-submissions"), Times.Once);
    }

    [Fact]
    public async Task Submit_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "", // Invalid - empty
            SubmissionImage = null
        };

        _controller.ModelState.AddModelError("SubmissionText", "Submission text is required");

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid input", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult.StatusCode.Should().Be(401);

        var value = unauthorizedResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid user", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(999))
            .ReturnsAsync(default(ChallengeParticipation));

        // Act
        var result = await _controller.Submit(999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Participation not found", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnBadRequest_WhenChallengeIsClosed()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Joined,
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Closed Challenge",
                Status = ChallengeStatus.Closed,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1)
            }
        };

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge is closed", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnBadRequest_WhenChallengeNotActive()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Joined,
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Future Challenge",
                Status = ChallengeStatus.Open,
                StartDate = DateTime.UtcNow.AddDays(5), // Starts in future
                EndDate = DateTime.UtcNow.AddDays(15)
            }
        };

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge is not active", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnBadRequest_WhenAlreadySubmitted()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.PendingApproval, // Already submitted
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Test Challenge",
                Status = ChallengeStatus.Open,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5)
            }
        };

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Challenge already submitted or completed", message);
    }

    [Fact]
    public async Task Submit_ShouldReturnInternalServerError_WhenFileUploadFails()
    {
        // Arrange
        var participation = new ChallengeParticipation
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Joined,
            Challenge = new Challenge
            {
                ChallengeId = 1,
                Title = "Test Challenge",
                Status = ChallengeStatus.Open,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5)
            }
        };

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission",
            SubmissionImage = mockFile.Object
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(participation);
        _mockFileStorage.Setup(x => x.UploadAsync(mockFile.Object, "challenge-submissions"))
            .ThrowsAsync(new Exception("Upload failed"));

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Failed to upload image", message);
        Assert.Equal("Upload failed", error);
    }

    [Fact]
    public async Task Submit_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new SubmitChallengeRequest
        {
            SubmissionText = "Test submission"
        };

        _mockParticipationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Submit(1, request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.Equal("Database error", error);
    }

    #endregion
}

