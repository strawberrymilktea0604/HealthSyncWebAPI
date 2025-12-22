using System.Security.Claims;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class LeaderboardControllerTests
{
    private readonly Mock<ILeaderboardUpdateJob> _mockLeaderboardUpdateJob;
    private readonly Mock<ILogger<LeaderboardController>> _mockLogger;
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _mockLeaderboardUpdateJob = new Mock<ILeaderboardUpdateJob>();
        _mockLogger = new Mock<ILogger<LeaderboardController>>();
        _controller = new LeaderboardController(_mockLeaderboardUpdateJob.Object, _mockLogger.Object);
        
        // Setup admin user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task UpdateAllUserContributionPoints_ValidAdmin_ReturnsOk()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateAllUserContributionPoints();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLeaderboardUpdateJob.Verify(j => j.UpdateUserContributionPointsAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAllUserContributionPoints_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.UpdateAllUserContributionPoints();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task UpdateAllUserContributionPoints_JobThrowsException_Returns500()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync()).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.UpdateAllUserContributionPoints();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_ValidUserId_ReturnsOk()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync(123)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUserContributionPoints(123);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLeaderboardUpdateJob.Verify(j => j.UpdateUserContributionPointsAsync(123), Times.Once);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_InvalidUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UpdateUserContributionPoints(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_NegativeUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UpdateUserContributionPoints(-1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.UpdateUserContributionPoints(123);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_JobThrowsException_Returns500()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync(123)).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.UpdateUserContributionPoints(123);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAllUserContributionPoints_LogsInformation()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync()).Returns(Task.CompletedTask);

        // Act
        await _controller.UpdateAllUserContributionPoints();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Triggering leaderboard update job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateUserContributionPoints_LogsInformationForSpecificUser()
    {
        // Arrange
        _mockLeaderboardUpdateJob.Setup(j => j.UpdateUserContributionPointsAsync(123)).Returns(Task.CompletedTask);

        // Act
        await _controller.UpdateUserContributionPoints(123);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UserId 123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
