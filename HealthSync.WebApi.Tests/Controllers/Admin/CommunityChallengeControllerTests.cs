using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class CommunityChallengeControllerTests : IDisposable
{
    private readonly CommunityChallengeController _controller;
    private readonly Mock<IChallengeAdminService> _mockChallengeAdminService;
    private readonly Mock<ILogger<CommunityChallengeController>> _mockLogger;

    public CommunityChallengeControllerTests()
    {
        // Setup mocks
        _mockChallengeAdminService = new Mock<IChallengeAdminService>();
        _mockLogger = new Mock<ILogger<CommunityChallengeController>>();

        // Create controller
        _controller = new CommunityChallengeController(
            _mockChallengeAdminService.Object,
            _mockLogger.Object);

        // Setup authenticated admin user
        var adminClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var adminIdentity = new ClaimsIdentity(adminClaims);
        var adminPrincipal = new ClaimsPrincipal(adminIdentity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminPrincipal }
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region CreateChallenge Tests

    [Fact]
    public async Task CreateChallenge_ShouldCreateChallengeSuccessfully()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "New Challenge",
            Description = "Test challenge description",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Criteria = "Complete 50km running",
            MaxParticipants = 100,
            RewardDescription = "Certificate"
        };

        var expectedResult = new ChallengeDto
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
            RewardDescription = request.RewardDescription
        };

        _mockChallengeAdminService.Setup(x => x.CreateChallengeAsync(request, 1))
            .ReturnsAsync((true, expectedResult, "Challenge created successfully"));

        // Act
        var result = await _controller.CreateChallenge(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetChallenge));

        var value = createdResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(expectedResult, data);
        Assert.Equal("Challenge created successfully", message);
    }

    [Fact]
    public async Task CreateChallenge_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "", // Invalid - empty title
            Description = "Test description"
        };

        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateChallenge(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var errors = value.GetType().GetProperty("errors")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid input data", message);
        Assert.NotNull(errors);
    }

    [Fact]
    public async Task CreateChallenge_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateChallengeRequest
        {
            Title = "Test Challenge",
            Description = "Test description"
        };

        // Act
        var result = await _controller.CreateChallenge(request);

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
        Assert.Equal("Invalid admin ID", message);
    }

    [Fact]
    public async Task CreateChallenge_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "Test Challenge",
            Description = "Test description",
            ChallengeType = ChallengeType.Workout,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        _mockChallengeAdminService.Setup(x => x.CreateChallengeAsync(request, 1))
            .ReturnsAsync((false, null, "Validation failed"));

        // Act
        var result = await _controller.CreateChallenge(request);

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
        Assert.Equal("Validation failed", message);
    }

    [Fact]
    public async Task CreateChallenge_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new CreateChallengeRequest
        {
            Title = "Test Challenge",
            Description = "Test description"
        };

        _mockChallengeAdminService.Setup(x => x.CreateChallengeAsync(request, 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateChallenge(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred while creating the challenge", message);
    }

    #endregion

    #region GetChallenge Tests

    [Fact]
    public async Task GetChallenge_ShouldReturnChallengeSuccessfully()
    {
        // Arrange
        var expectedChallenge = new ChallengeDto
        {
            ChallengeId = 1,
            Title = "Test Challenge",
            Description = "Test description",
            Status = ChallengeStatus.Open
        };

        _mockChallengeAdminService.Setup(x => x.GetChallengeAsync(1))
            .ReturnsAsync((true, expectedChallenge, "Challenge retrieved successfully"));

        // Act
        var result = await _controller.GetChallenge(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(expectedChallenge, data);
        Assert.Equal("Challenge retrieved successfully", message);
    }

    [Fact]
    public async Task GetChallenge_ShouldReturnNotFound_WhenChallengeDoesNotExist()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.GetChallengeAsync(999))
            .ReturnsAsync((false, null, "Challenge not found"));

        // Act
        var result = await _controller.GetChallenge(999);

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
    public async Task GetChallenge_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.GetChallengeAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetChallenge(1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred while retrieving the challenge", message);
    }

    #endregion

    #region GetAllChallenges Tests

    [Fact]
    public async Task GetAllChallenges_ShouldReturnChallengesSuccessfully()
    {
        // Arrange
        var challenges = new List<ChallengeDto>
        {
            new ChallengeDto { ChallengeId = 1, Title = "Challenge 1" },
            new ChallengeDto { ChallengeId = 2, Title = "Challenge 2" }
        };

        var paginatedResult = new PaginatedResult<ChallengeDto>
        {
            Items = challenges,
            TotalItems = 2,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 1,
            HasNext = false,
            HasPrevious = false
        };

        _mockChallengeAdminService.Setup(x => x.GetAllChallengesAsync(1, 20))
            .ReturnsAsync((true, paginatedResult, "Challenges retrieved successfully"));

        // Act
        var result = await _controller.GetAllChallenges(1, 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(paginatedResult, data);
        Assert.Equal("Challenges retrieved successfully", message);
    }

    [Fact]
    public async Task GetAllChallenges_ShouldApplyDefaultPaginationValues()
    {
        // Arrange
        var challenges = new List<ChallengeDto>
        {
            new ChallengeDto { ChallengeId = 1, Title = "Challenge 1" }
        };

        var paginatedResult = new PaginatedResult<ChallengeDto>
        {
            Items = challenges,
            TotalItems = 1,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 1,
            HasNext = false,
            HasPrevious = false
        };

        _mockChallengeAdminService.Setup(x => x.GetAllChallengesAsync(1, 20))
            .ReturnsAsync((true, paginatedResult, "Challenges retrieved successfully"));

        // Act - Call without parameters (should use defaults)
        var result = await _controller.GetAllChallenges();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockChallengeAdminService.Verify(x => x.GetAllChallengesAsync(1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllChallenges_ShouldValidatePaginationParameters()
    {
        // Arrange
        var challenges = new List<ChallengeDto>();
        var paginatedResult = new PaginatedResult<ChallengeDto>
        {
            Items = challenges,
            TotalItems = 0,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 0,
            HasNext = false,
            HasPrevious = false
        };

        _mockChallengeAdminService.Setup(x => x.GetAllChallengesAsync(1, 20))
            .ReturnsAsync((true, paginatedResult, "Challenges retrieved successfully"));

        // Act - Call with invalid parameters (should be corrected)
        var result = await _controller.GetAllChallenges(0, 150);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockChallengeAdminService.Verify(x => x.GetAllChallengesAsync(1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllChallenges_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.GetAllChallengesAsync(1, 20))
            .ReturnsAsync((false, null, "Service error"));

        // Act
        var result = await _controller.GetAllChallenges(1, 20);

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
        Assert.Equal("Service error", message);
    }

    #endregion

    #region UpdateChallenge Tests

    [Fact]
    public async Task UpdateChallenge_ShouldUpdateChallengeSuccessfully()
    {
        // Arrange
        var request = new UpdateChallengeRequest
        {
            Title = "Updated Challenge",
            Description = "Updated description",
            MaxParticipants = 150
        };

        var updatedChallenge = new ChallengeDto
        {
            ChallengeId = 1,
            Title = "Updated Challenge",
            Description = "Updated description",
            MaxParticipants = 150
        };

        _mockChallengeAdminService.Setup(x => x.UpdateChallengeAsync(1, request, 1))
            .ReturnsAsync((true, updatedChallenge, "Challenge updated successfully"));

        // Act
        var result = await _controller.UpdateChallenge(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(updatedChallenge, data);
        Assert.Equal("Challenge updated successfully", message);
    }

    [Fact]
    public async Task UpdateChallenge_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new UpdateChallengeRequest
        {
            Title = "Updated Title"
        };

        // Act
        var result = await _controller.UpdateChallenge(1, request);

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
        Assert.Equal("Invalid admin ID", message);
    }

    [Fact]
    public async Task UpdateChallenge_ShouldReturnNotFound_WhenChallengeDoesNotExist()
    {
        // Arrange
        var request = new UpdateChallengeRequest
        {
            Title = "Updated Title"
        };

        _mockChallengeAdminService.Setup(x => x.UpdateChallengeAsync(999, request, 1))
            .ReturnsAsync((false, null, "Challenge not found"));

        // Act
        var result = await _controller.UpdateChallenge(999, request);

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

    #endregion

    #region DeleteChallenge Tests

    [Fact]
    public async Task DeleteChallenge_ShouldDeleteChallengeSuccessfully()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.DeleteChallengeAsync(1, 1))
            .ReturnsAsync((true, "Challenge deleted successfully"));

        // Act
        var result = await _controller.DeleteChallenge(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull();
        noContentResult.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteChallenge_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.DeleteChallenge(1);

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
        Assert.Equal("Invalid admin ID", message);
    }

    [Fact]
    public async Task DeleteChallenge_ShouldReturnNotFound_WhenChallengeDoesNotExist()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.DeleteChallengeAsync(999, 1))
            .ReturnsAsync((false, "Challenge not found"));

        // Act
        var result = await _controller.DeleteChallenge(999);

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

    #endregion

    #region GetPendingApprovals Tests

    [Fact]
    public async Task GetPendingApprovals_ShouldReturnPendingApprovalsSuccessfully()
    {
        // Arrange
        var pendingApprovals = new List<ParticipationDto>
        {
            new ParticipationDto
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.PendingApproval,
                SubmissionText = "Completed challenge",
                SubmittedAt = DateTime.UtcNow
            }
        };

        _mockChallengeAdminService.Setup(x => x.GetPendingApprovalsAsync(1))
            .ReturnsAsync((true, pendingApprovals, "Pending approvals retrieved successfully"));

        // Act
        var result = await _controller.GetPendingApprovals(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value) as IEnumerable<ParticipationDto>;
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal("Pending approvals retrieved successfully", message);
    }

    [Fact]
    public async Task GetPendingApprovals_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.GetPendingApprovalsAsync(1))
            .ReturnsAsync((false, null, "Service error"));

        // Act
        var result = await _controller.GetPendingApprovals(1);

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
        Assert.Equal("Service error", message);
    }

    #endregion

    #region ReviewParticipation Tests

    [Fact]
    public async Task ReviewParticipation_ShouldApproveParticipationSuccessfully()
    {
        // Arrange
        var request = new ReviewParticipationRequest
        {
            Approved = true,
            ReviewNotes = "Great job!"
        };

        var updatedParticipation = new ParticipationDto
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Completed,
            ReviewNotes = "Great job!",
            CompletedAt = DateTime.UtcNow
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, request, 1))
            .ReturnsAsync((true, updatedParticipation, "Participation approved successfully"));

        // Act
        var result = await _controller.ReviewParticipation(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(updatedParticipation, data);
        Assert.Equal("Participation approved successfully", message);
    }

    [Fact]
    public async Task ReviewParticipation_ShouldRejectParticipationSuccessfully()
    {
        // Arrange
        var request = new ReviewParticipationRequest
        {
            Approved = false,
            ReviewNotes = "Needs more work"
        };

        var updatedParticipation = new ParticipationDto
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Failed,
            ReviewNotes = "Needs more work"
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, request, 1))
            .ReturnsAsync((true, updatedParticipation, "Participation rejected"));

        // Act
        var result = await _controller.ReviewParticipation(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(updatedParticipation, data);
        Assert.Equal("Participation rejected", message);
    }

    [Fact]
    public async Task ReviewParticipation_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new ReviewParticipationRequest
        {
            Approved = true
        };

        // Act
        var result = await _controller.ReviewParticipation(1, request);

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
        Assert.Equal("Invalid admin ID", message);
    }

    [Fact]
    public async Task ReviewParticipation_ShouldReturnNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        var request = new ReviewParticipationRequest
        {
            Approved = true
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(999, request, 1))
            .ReturnsAsync((false, null, "Participation not found"));

        // Act
        var result = await _controller.ReviewParticipation(999, request);

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

    #endregion

    #region GetAllPendingApprovals Tests

    [Fact]
    public async Task GetAllPendingApprovals_ShouldReturnPendingApprovalsSuccessfully()
    {
        // Arrange
        var pendingApprovals = new List<ParticipationDto>
        {
            new ParticipationDto
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.PendingApproval,
                SubmissionText = "Completed challenge 1"
            },
            new ParticipationDto
            {
                ParticipationId = 2,
                ChallengeId = 2,
                UserId = 2,
                Status = ParticipationStatus.PendingApproval,
                SubmissionText = "Completed challenge 2"
            }
        };

        var paginatedResult = new PaginatedResult<ParticipationDto>
        {
            Items = pendingApprovals,
            TotalItems = 2,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 1,
            HasNext = false,
            HasPrevious = false
        };

        _mockChallengeAdminService.Setup(x => x.GetAllPendingApprovalsAsync(1, 20))
            .ReturnsAsync((true, paginatedResult, "Pending approvals retrieved successfully"));

        // Act
        var result = await _controller.GetAllPendingApprovals(1, 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(paginatedResult, data);
        Assert.Equal("Pending approvals retrieved successfully", message);
    }

    [Fact]
    public async Task GetAllPendingApprovals_ShouldApplyDefaultPaginationValues()
    {
        // Arrange
        var pendingApprovals = new List<ParticipationDto>();
        var paginatedResult = new PaginatedResult<ParticipationDto>
        {
            Items = pendingApprovals,
            TotalItems = 0,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 0,
            HasNext = false,
            HasPrevious = false
        };

        _mockChallengeAdminService.Setup(x => x.GetAllPendingApprovalsAsync(1, 20))
            .ReturnsAsync((true, paginatedResult, "Pending approvals retrieved successfully"));

        // Act - Call without parameters
        var result = await _controller.GetAllPendingApprovals();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockChallengeAdminService.Verify(x => x.GetAllPendingApprovalsAsync(1, 20), Times.Once);
    }

    #endregion

    #region GetChallengeParticipants Tests

    [Fact]
    public async Task GetChallengeParticipants_ShouldReturnParticipantsSuccessfully()
    {
        // Arrange
        var participants = new List<ParticipationDto>
        {
            new ParticipationDto
            {
                ParticipationId = 1,
                ChallengeId = 1,
                UserId = 1,
                Status = ParticipationStatus.Joined,
                JoinedDate = DateTime.UtcNow.AddDays(-5)
            },
            new ParticipationDto
            {
                ParticipationId = 2,
                ChallengeId = 1,
                UserId = 2,
                Status = ParticipationStatus.Completed,
                JoinedDate = DateTime.UtcNow.AddDays(-3),
                CompletedAt = DateTime.UtcNow
            }
        };

        _mockChallengeAdminService.Setup(x => x.GetChallengeParticipantsAsync(1))
            .ReturnsAsync((true, participants, "Participants retrieved successfully"));

        // Act
        var result = await _controller.GetChallengeParticipants(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value) as IEnumerable<ParticipationDto>;
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.NotNull(data);
        Assert.Equal(2, data.Count());
        Assert.Equal("Participants retrieved successfully", message);
    }

    [Fact]
    public async Task GetChallengeParticipants_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        _mockChallengeAdminService.Setup(x => x.GetChallengeParticipantsAsync(1))
            .ReturnsAsync((false, null, "Service error"));

        // Act
        var result = await _controller.GetChallengeParticipants(1);

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
        Assert.Equal("Service error", message);
    }

    #endregion

    #region ApproveParticipation Tests

    [Fact]
    public async Task ApproveParticipation_ShouldApproveParticipationSuccessfully()
    {
        // Arrange
        var approvalRequest = new ReviewParticipationRequest
        {
            Approved = true,
            ReviewNotes = "Excellent work!"
        };

        var updatedParticipation = new ParticipationDto
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Completed,
            ReviewNotes = "Excellent work!",
            CompletedAt = DateTime.UtcNow
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, It.IsAny<ReviewParticipationRequest>(), 1))
            .ReturnsAsync((true, updatedParticipation, "Participation approved successfully"));

        // Act
        var result = await _controller.ApproveParticipation(1, approvalRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(updatedParticipation, data);
        Assert.Equal("Participation approved successfully", message);
    }

    [Fact]
    public async Task ApproveParticipation_ShouldApproveWithDefaultRequest_WhenNoRequestProvided()
    {
        // Arrange
        var defaultApprovalRequest = new ReviewParticipationRequest
        {
            Approved = true,
            ReviewNotes = null
        };

        var updatedParticipation = new ParticipationDto
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Completed
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, It.IsAny<ReviewParticipationRequest>(), 1))
            .ReturnsAsync((true, updatedParticipation, "Participation approved successfully"));

        // Act - Call without request parameter
        var result = await _controller.ApproveParticipation(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockChallengeAdminService.Verify(x => x.ReviewParticipationAsync(1, It.Is<ReviewParticipationRequest>(
            r => r.Approved == true && r.ReviewNotes == null), 1), Times.Once);
    }

    #endregion

    #region RejectParticipation Tests

    [Fact]
    public async Task RejectParticipation_ShouldRejectParticipationSuccessfully()
    {
        // Arrange
        var request = new ReviewParticipationRequest
        {
            Approved = false, // Will be overridden to false
            ReviewNotes = "Needs improvement"
        };

        var updatedParticipation = new ParticipationDto
        {
            ParticipationId = 1,
            ChallengeId = 1,
            UserId = 1,
            Status = ParticipationStatus.Failed,
            ReviewNotes = "Needs improvement"
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, It.Is<ReviewParticipationRequest>(
            r => r.Approved == false && r.ReviewNotes == "Needs improvement"), 1))
            .ReturnsAsync((true, updatedParticipation, "Participation rejected"));

        // Act
        var result = await _controller.RejectParticipation(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal(updatedParticipation, data);
        Assert.Equal("Participation rejected", message);
    }

    [Fact]
    public async Task RejectParticipation_ShouldForceApprovedToFalse()
    {
        // Arrange
        var request = new ReviewParticipationRequest
        {
            Approved = true, // This should be overridden to false
            ReviewNotes = "Rejected"
        };

        _mockChallengeAdminService.Setup(x => x.ReviewParticipationAsync(1, It.Is<ReviewParticipationRequest>(
            r => r.Approved == false), 1)) // Should be false regardless of input
            .ReturnsAsync((true, new ParticipationDto(), "Participation rejected"));

        // Act
        var result = await _controller.RejectParticipation(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockChallengeAdminService.Verify(x => x.ReviewParticipationAsync(1, It.Is<ReviewParticipationRequest>(
            r => r.Approved == false), 1), Times.Once);
    }

    #endregion
}

