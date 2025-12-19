using FluentAssertions;
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
}