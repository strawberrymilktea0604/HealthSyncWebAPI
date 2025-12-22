using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Leaderboard;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class LeaderboardControllerTests
{
    private readonly Mock<ILeaderboardService> _mockLeaderboardService;
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _mockLeaderboardService = new Mock<ILeaderboardService>();
        _controller = new LeaderboardController(_mockLeaderboardService.Object);
    }

    private void SetupUserClaims(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetTopLeaderboard_ShouldReturnOk_WithTopUsers()
    {
        // Arrange
        var limit = 10;
        var topUsers = new List<LeaderboardUserDto>
        {
            new LeaderboardUserDto { UserId = 1, FullName = "User 1", ContributionPoints = 100 },
            new LeaderboardUserDto { UserId = 2, FullName = "User 2", ContributionPoints = 90 }
        };

        _mockLeaderboardService
            .Setup(s => s.GetTopUsersByContributionPointsAsync(limit))
            .ReturnsAsync(topUsers);

        // Act
        var result = await _controller.GetTopLeaderboard(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = topUsers
        });
    }

    [Fact]
    public async Task GetTopLeaderboard_ShouldReturnBadRequest_WhenLimitTooLow()
    {
        // Arrange
        var limit = 0;

        // Act
        var result = await _controller.GetTopLeaderboard(limit);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        var value = badRequestResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Limit must be between 1 and 100"
        });
    }

    [Fact]
    public async Task GetTopLeaderboard_ShouldReturnBadRequest_WhenLimitTooHigh()
    {
        // Arrange
        var limit = 101;

        // Act
        var result = await _controller.GetTopLeaderboard(limit);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        var value = badRequestResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Limit must be between 1 and 100"
        });
    }

    [Fact]
    public async Task GetMyRanking_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        SetupUserClaims(1);
        var userRank = new UserRankDto
        {
            UserId = 1,
            UserName = "Test User",
            TotalPoints = 150,
            RankPosition = 5,
            UpdatedAt = DateTime.UtcNow
        };

        _mockLeaderboardService
            .Setup(s => s.GetUserRankAsync(1))
            .ReturnsAsync(userRank);

        // Act
        var result = await _controller.GetMyRanking();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var dataProperty = response.GetType().GetProperty("data")?.GetValue(response);
        successProperty.Should().Be(true);
        dataProperty.Should().Be(userRank);
    }

    [Fact]
    public async Task GetMyRanking_ShouldReturnNotFound_WhenUserNotFound()
    {
        // Arrange
        SetupUserClaims(1);
        _mockLeaderboardService
            .Setup(s => s.GetUserRankAsync(1))
            .ReturnsAsync((UserRankDto?)null);

        // Act
        var result = await _controller.GetMyRanking();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.StatusCode.Should().Be(404);
        var response = notFoundResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Leaderboard entry not found");
    }

    [Fact]
    public async Task GetMyRanking_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        // No claims setup

        // Act
        var result = await _controller.GetMyRanking();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnOk_WithPaginatedResults()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 20;
        var paginatedResult = new PaginatedResult<LeaderboardEntryDto>
        {
            Items = new List<LeaderboardEntryDto>
            {
                new LeaderboardEntryDto { UserId = 1, UserName = "User 1", TotalPoints = 200, RankPosition = 1 }
            },
            TotalItems = 1,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = 1
        };

        _mockLeaderboardService
            .Setup(s => s.GetLeaderboardAsync(pageNumber, pageSize))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetLeaderboard(pageNumber, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var dataProperty = response.GetType().GetProperty("data")?.GetValue(response);
        successProperty.Should().Be(true);
        dataProperty.Should().Be(paginatedResult);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnBadRequest_WhenPageNumberInvalid()
    {
        // Arrange
        var pageNumber = 0;
        var pageSize = 20;

        // Act
        var result = await _controller.GetLeaderboard(pageNumber, pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Page number must be >= 1");
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnBadRequest_WhenPageSizeTooLow()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 0;

        // Act
        var result = await _controller.GetLeaderboard(pageNumber, pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnBadRequest_WhenPageSizeTooHigh()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 101;

        // Act
        var result = await _controller.GetLeaderboard(pageNumber, pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetTopLeaderboard_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockLeaderboardService
            .Setup(s => s.GetTopUsersByContributionPointsAsync(10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTopLeaderboard(10);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = (ObjectResult)result;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("An error occurred");
    }
}