using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HealthSync.WebApi.Controllers.Admin;
using HealthSync.Application.Interfaces;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardAdminService> _mockDashboardService;
    private readonly Mock<ILogger<DashboardController>> _mockLogger;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockDashboardService = new Mock<IDashboardAdminService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_mockDashboardService.Object, _mockLogger.Object);

        // Setup user claims for admin
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetStats_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var fakeData = new
        {
            TotalActiveUsers = 150,
            NewUsersThisMonth = 25,
            WorkoutsLoggedToday = 45
        };

        _mockDashboardService.Setup(s => s.GetDashboardStatsAsync())
                            .ReturnsAsync((true, fakeData, "Dashboard stats retrieved successfully"));

        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Check response structure using reflection
        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var dataValue = dataProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(dataValue);
        Assert.NotNull(messageValue);

        Assert.True((bool)successValue);
        Assert.Equal(fakeData, dataValue);
        Assert.Equal("Dashboard stats retrieved successfully", messageValue);
    }

    [Fact]
    public async Task GetStats_ShouldReturnInternalServerError_WhenServiceReturnsFailure()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetDashboardStatsAsync())
                            .ReturnsAsync((false, null, "Database connection failed"));

        // Act
        var result = await _controller.GetStats();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);

        var response = statusResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(messageValue);

        Assert.False((bool)successValue);
        Assert.Equal("Database connection failed", messageValue);
    }

    [Fact]
    public async Task GetStats_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetDashboardStatsAsync())
                            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetStats();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);

        var response = statusResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(messageValue);

        Assert.False((bool)successValue);
        Assert.Contains("An error occurred while retrieving dashboard statistics", messageValue.ToString());
    }

    [Fact]
    public async Task GetDetailedStats_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var fakeData = new
        {
            TotalActiveUsers = 150,
            NewUsersThisMonth = 25,
            WorkoutsLoggedToday = 45,
            NutritionLogsToday = 32,
            ForumPostsThisMonth = 120,
            ForumRepliesThisMonth = 340,
            OpenChallenges = 5,
            PendingSubmissions = 12
        };

        _mockDashboardService.Setup(s => s.GetDetailedStatsAsync())
                            .ReturnsAsync((true, fakeData, "Detailed stats retrieved successfully"));

        // Act
        var result = await _controller.GetDetailedStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var dataValue = dataProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(dataValue);
        Assert.NotNull(messageValue);

        Assert.True((bool)successValue);
        Assert.Equal(fakeData, dataValue);
        Assert.Equal("Detailed stats retrieved successfully", messageValue);
    }

    [Fact]
    public async Task GetTopContent_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var fakeData = new
        {
            TopExercises = new[]
            {
                new { Name = "Push Up", UsageCount = 1250 },
                new { Name = "Squat", UsageCount = 980 }
            },
            TopForumCategories = new[]
            {
                new { Name = "General Discussion", ActivityCount = 450 },
                new { Name = "Workout Tips", ActivityCount = 320 }
            }
        };

        _mockDashboardService.Setup(s => s.GetTopContentAsync())
                            .ReturnsAsync((true, fakeData, "Top content retrieved successfully"));

        // Act
        var result = await _controller.GetTopContent();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var dataValue = dataProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(dataValue);
        Assert.NotNull(messageValue);

        Assert.True((bool)successValue);
        Assert.Equal(fakeData, dataValue);
        Assert.Equal("Top content retrieved successfully", messageValue);
    }

    [Fact]
    public async Task GetUsersByContributionPoints_ShouldReturnOk_WhenServiceReturnsSuccess()
    {
        // Arrange
        var fakeData = new[]
        {
            new { UserId = 1, FullName = "John Doe", ContributionPoints = 150 },
            new { UserId = 2, FullName = "Jane Smith", ContributionPoints = 120 },
            new { UserId = 3, FullName = "Bob Johnson", ContributionPoints = 95 }
        };

        _mockDashboardService.Setup(s => s.GetUsersByContributionPointsAsync())
                            .ReturnsAsync((true, fakeData, "Users by contribution points retrieved successfully"));

        // Act
        var result = await _controller.GetUsersByContributionPoints();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var dataValue = dataProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(dataValue);
        Assert.NotNull(messageValue);

        Assert.True((bool)successValue);
        Assert.Equal(fakeData, dataValue);
        Assert.Equal("Users by contribution points retrieved successfully", messageValue);
    }
}

