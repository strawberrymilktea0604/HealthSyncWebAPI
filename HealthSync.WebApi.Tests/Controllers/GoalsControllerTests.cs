using FluentAssertions;
using FluentValidation;
using HealthSync.Domain.Entities;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Goals;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace HealthSync.WebApi.Tests.Controllers;

public class GoalsControllerTests
{
    private readonly Mock<IGoalService> _mockGoalService;
    private readonly GoalsController _controller;

    public GoalsControllerTests()
    {
        _mockGoalService = new Mock<IGoalService>();
        _controller = new GoalsController(_mockGoalService.Object);

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

    [Fact]
    public async Task CreateGoal_ShouldReturnCreated_WhenGoalCreated()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = HealthSync.Domain.Entities.GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var expectedGoal = new GoalDto
        {
            GoalId = 1,
            UserId = 1,
            GoalType = HealthSync.Domain.Entities.GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 3, 1),
            Status = HealthSync.Domain.Entities.GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            ProgressRecords = new List<ProgressRecordDto>()
        };

        _mockGoalService
            .Setup(s => s.CreateGoalAsync(request, userId))
            .ReturnsAsync(expectedGoal);

        // Act
        var result = await _controller.CreateGoal(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var json = JsonSerializer.Serialize(createdResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("GoalId").GetInt32().Should().Be(1);
        data.GetProperty("GoalType").GetInt32().Should().Be(0); // WeightLoss = 0
        data.GetProperty("TargetValue").GetDecimal().Should().Be(70.0m);
    }

    [Fact]
    public async Task GetGoal_ShouldReturnOk_WhenGoalExists()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        var expectedGoal = new GoalDto
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 3, 1),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            ProgressRecords = new List<ProgressRecordDto>
            {
                new ProgressRecordDto
                {
                    ProgressRecordId = 1,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 15),
                    RecordedValue = 72.5m,
                    WeightKg = 72.5m,
                    Notes = "Initial weight",
                    CreatedAt = DateTime.UtcNow
                }
            },
            UpdatedAt = null
        };

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ReturnsAsync(expectedGoal);

        // Act
        var result = await _controller.GetGoal(goalId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("GoalId").GetInt32().Should().Be(goalId);
        data.GetProperty("ProgressRecords").EnumerateArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserGoals_ShouldReturnOk_WhenGoalsRetrieved()
    {
        // Arrange
        var userId = 1;

        var goals = new List<GoalDto>
        {
            new GoalDto
            {
                GoalId = 1,
                UserId = userId,
                GoalType = GoalType.WeightLoss,
                TargetValue = 70.0m,
                Unit = "kg",
                StartDate = new DateTime(2025, 12, 1),
                EndDate = new DateTime(2026, 3, 1),
                Status = GoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            },
            new GoalDto
            {
                GoalId = 2,
                UserId = userId,
                GoalType = GoalType.WeightGain,
                TargetValue = 75.0m,
                Unit = "kg",
                StartDate = new DateTime(2025, 11, 1),
                EndDate = new DateTime(2026, 2, 1),
                Status = GoalStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            }
        };

        _mockGoalService
            .Setup(s => s.GetUserGoalsAsync(userId))
            .ReturnsAsync(goals);

        // Act
        var result = await _controller.GetMyGoals();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.EnumerateArray().Should().HaveCount(2);
        data[0].GetProperty("GoalType").GetInt32().Should().Be(0); // WeightLoss
        data[1].GetProperty("Status").GetInt32().Should().Be(1); // Completed
    }

    [Fact]
    public async Task RecordProgress_ShouldReturnOk_WhenProgressRecorded()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 71.5m,
            WeightKg = 71.5m,
            WaistCm = 85.0m,
            ChestCm = 95.0m,
            HipCm = 100.0m,
            Notes = "Lost 1kg this week"
        };

        var expectedProgress = new ProgressRecordDto
        {
            ProgressRecordId = 1,
            GoalId = goalId,
            RecordDate = new DateTime(2025, 12, 20),
            RecordedValue = 71.5m,
            WeightKg = 71.5m,
            WaistCm = 85.0m,
            ChestCm = 95.0m,
            HipCm = 100.0m,
            Notes = "Lost 1kg this week",
            CreatedAt = DateTime.UtcNow
        };

        _mockGoalService
            .Setup(s => s.RecordProgressAsync(request, userId))
            .ReturnsAsync(expectedProgress);

        // Act
        var result = await _controller.RecordProgress(goalId, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var json = JsonSerializer.Serialize(createdResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("RecordedValue").GetDecimal().Should().Be(71.5m);
        data.GetProperty("Notes").GetString().Should().Be("Lost 1kg this week");
    }

    [Fact]
    public async Task GetProgressChart_ShouldReturnOk_WhenChartDataRetrieved()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        var chartData = new ChartDataDto
        {
            GoalId = goalId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 3, 1),
            Status = GoalStatus.InProgress,
            ProgressPercent = 25.0m,
            ProgressRecords = new List<ProgressRecordDto>
            {
                new ProgressRecordDto
                {
                    ProgressRecordId = 1,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 1),
                    RecordedValue = 75.0m,
                    Notes = "Starting weight",
                    CreatedAt = DateTime.UtcNow
                },
                new ProgressRecordDto
                {
                    ProgressRecordId = 2,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 15),
                    RecordedValue = 73.5m,
                    Notes = "Lost 1.5kg",
                    CreatedAt = DateTime.UtcNow
                },
                new ProgressRecordDto
                {
                    ProgressRecordId = 3,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 20),
                    RecordedValue = 71.5m,
                    Notes = "Continuing progress",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockGoalService
            .Setup(s => s.GetProgressChartAsync(goalId, userId))
            .ReturnsAsync(chartData);

        // Act
        var result = await _controller.GetProgressChart(goalId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("GoalId").GetInt32().Should().Be(goalId);
        data.GetProperty("ProgressRecords").EnumerateArray().Should().HaveCount(3);
        data.GetProperty("ProgressPercent").GetDecimal().Should().Be(25.0m);
    }

    [Fact]
    public async Task CreateGoal_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = -10.0m, // Invalid negative value
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(-1), // Past date
            EndDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _controller.CreateGoal(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Validation failed");
        root.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateGoal_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        // Setup unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.CreateGoal(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User not authenticated");
    }

    [Fact]
    public async Task CreateGoal_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        _mockGoalService
            .Setup(s => s.CreateGoalAsync(request, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateGoal(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while creating the goal");
    }

    [Fact]
    public async Task GetGoal_ShouldReturnNotFound_WhenGoalDoesNotExist()
    {
        // Arrange
        var goalId = 999;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ThrowsAsync(new ValidationException("Goal not found"));

        // Act
        var result = await _controller.GetGoal(goalId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Goal not found");
    }

    [Fact]
    public async Task GetGoal_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetGoal(goalId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving the goal");
    }

    [Fact]
    public async Task GetMyGoals_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var userId = 1;

        _mockGoalService
            .Setup(s => s.GetUserGoalsAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMyGoals();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving goals");
    }

    [Fact]
    public async Task UpdateGoal_ShouldReturnOk_WhenGoalUpdated()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;
        var request = new UpdateGoalRequest
        {
            TargetValue = 68.0m,
            StartDate = new DateTime(2025, 12, 1),
            EndDate = DateTime.UtcNow.Date.AddDays(60),
            Unit = "kg"
        };

        var updatedGoal = new GoalDto
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 68.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 2, 1),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };

        _mockGoalService
            .Setup(s => s.UpdateGoalAsync(goalId, request, userId))
            .ReturnsAsync(updatedGoal);

        // Act
        var result = await _controller.UpdateGoal(goalId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("GoalId").GetInt32().Should().Be(goalId);
        data.GetProperty("TargetValue").GetDecimal().Should().Be(68.0m);
    }

    [Fact]
    public async Task UpdateGoal_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            TargetValue = -5.0m, // Invalid
            EndDate = DateTime.UtcNow.Date.AddDays(-1) // Past date
        };

        // Act
        var result = await _controller.UpdateGoal(goalId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Validation failed");
    }

    [Fact]
    public async Task UpdateGoal_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;
        var request = new UpdateGoalRequest
        {
            TargetValue = 68.0m,
            StartDate = new DateTime(2025, 12, 1),
            EndDate = DateTime.UtcNow.Date.AddDays(60),
            Unit = "kg"
        };

        _mockGoalService
            .Setup(s => s.UpdateGoalAsync(goalId, request, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateGoal(goalId, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving the goal");
    }

    [Fact]
    public async Task DeleteGoal_ShouldReturnNoContent_WhenGoalDeleted()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.DeleteGoalAsync(goalId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGoal(goalId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGoal_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.DeleteGoalAsync(goalId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteGoal(goalId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while deleting the goal");
    }

    [Fact]
    public async Task RecordProgress_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date.AddDays(1), // Future date - invalid
            RecordedValue = -10.0m // Invalid negative value
        };

        // Act
        var result = await _controller.RecordProgress(goalId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Validation failed");
    }

    [Fact]
    public async Task RecordProgress_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 71.5m
        };

        _mockGoalService
            .Setup(s => s.RecordProgressAsync(request, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.RecordProgress(goalId, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while deleting progress");
    }

    [Fact]
    public async Task GetProgressRecord_ShouldReturnOk_WhenProgressRecordExists()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;

        var goal = new GoalDto
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 3, 1),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            ProgressRecords = new List<ProgressRecordDto>
            {
                new ProgressRecordDto
                {
                    ProgressRecordId = recordId,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 15),
                    RecordedValue = 72.5m,
                    Notes = "Progress check",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ReturnsAsync(goal);

        // Act
        var result = await _controller.GetProgressRecord(goalId, recordId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("ProgressRecordId").GetInt32().Should().Be(recordId);
        data.GetProperty("RecordedValue").GetDecimal().Should().Be(72.5m);
    }

    [Fact]
    public async Task GetProgressRecord_ShouldReturnNotFound_WhenProgressRecordDoesNotExist()
    {
        // Arrange
        var goalId = 1;
        var recordId = 999;
        var userId = 1;

        var goal = new GoalDto
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 3, 1),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            ProgressRecords = new List<ProgressRecordDto>
            {
                new ProgressRecordDto
                {
                    ProgressRecordId = 1,
                    GoalId = goalId,
                    RecordDate = new DateTime(2025, 12, 15),
                    RecordedValue = 72.5m,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ReturnsAsync(goal);

        // Act
        var result = await _controller.GetProgressRecord(goalId, recordId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Progress record not found");
    }

    [Fact]
    public async Task GetProgressRecord_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.GetGoalByIdAsync(goalId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProgressRecord(goalId, recordId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving progress records");
    }

    [Fact]
    public async Task UpdateProgressRecord_ShouldReturnOk_WhenProgressRecordUpdated()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;
        var request = new UpdateProgressRequest
        {
            RecordedValue = 70.5m,
            Notes = "Updated progress"
        };

        var updatedProgress = new ProgressRecordDto
        {
            ProgressRecordId = recordId,
            GoalId = goalId,
            RecordDate = new DateTime(2025, 12, 15),
            RecordedValue = 70.5m,
            Notes = "Updated progress",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };

        _mockGoalService
            .Setup(s => s.UpdateProgressRecordAsync(recordId, request, userId))
            .ReturnsAsync(updatedProgress);

        // Act
        var result = await _controller.UpdateProgressRecord(goalId, recordId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("ProgressRecordId").GetInt32().Should().Be(recordId);
        data.GetProperty("RecordedValue").GetDecimal().Should().Be(70.5m);
        data.GetProperty("Notes").GetString().Should().Be("Updated progress");
    }

    [Fact]
    public async Task UpdateProgressRecord_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var request = new UpdateProgressRequest
        {
            RecordedValue = -15.0m // Invalid negative value
        };

        // Act
        var result = await _controller.UpdateProgressRecord(goalId, recordId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Validation failed");
    }

    [Fact]
    public async Task UpdateProgressRecord_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;
        var request = new UpdateProgressRequest
        {
            RecordedValue = 70.5m,
            Notes = "Updated progress"
        };

        _mockGoalService
            .Setup(s => s.UpdateProgressRecordAsync(recordId, request, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateProgressRecord(goalId, recordId, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while updating the goal");
    }

    [Fact]
    public async Task DeleteProgressRecord_ShouldReturnNoContent_WhenProgressRecordDeleted()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.DeleteProgressRecordAsync(recordId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProgressRecord(goalId, recordId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProgressRecord_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var recordId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.DeleteProgressRecordAsync(recordId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteProgressRecord(goalId, recordId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while deleting the progress record");
    }

    [Fact]
    public async Task GetProgressChart_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var goalId = 1;
        var userId = 1;

        _mockGoalService
            .Setup(s => s.GetProgressChartAsync(goalId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProgressChart(goalId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving chart data");
    }
}

