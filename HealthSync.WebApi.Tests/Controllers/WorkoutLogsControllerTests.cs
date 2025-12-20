using FluentAssertions;
using System.Text.Json;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class WorkoutLogsControllerTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Mock<IWorkoutLogService> _mockWorkoutLogService;
    private readonly Mock<ILogger<WorkoutLogsController>> _mockLogger;
    private readonly WorkoutLogsController _controller;

    public WorkoutLogsControllerTests()
    {
        _mockWorkoutLogService = new Mock<IWorkoutLogService>();
        _mockLogger = new Mock<ILogger<WorkoutLogsController>>();

        _controller = new WorkoutLogsController(_mockWorkoutLogService.Object, _mockLogger.Object);

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
    public async Task CreateWorkoutLog_ShouldReturnCreated_WhenCreationSucceeds()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = new DateTime(2025, 12, 19),
            Notes = "Chest day workout",
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 1,
                    Sets = 4,
                    Reps = 10,
                    WeightKg = 80,
                    RestSeconds = 90,
                    Rpe = 8,
                    OrderIndex = 1
                }
            }
        };

        var expectedResponse = new WorkoutLogResponse
        {
            WorkoutLogId = 1,
            UserId = 1,
            WorkoutDate = new DateTime(2025, 12, 19),
            TotalDurationMinutes = 30,
            EstimatedCaloriesBurned = 250,
            Notes = "Chest day workout",
            CreatedAt = DateTime.UtcNow,
            ExerciseSessions = new List<ExerciseSessionDto>
            {
                new ExerciseSessionDto
                {
                    ExerciseSessionId = 1,
                    ExerciseId = 1,
                    Sets = 4,
                    Reps = 10,
                    WeightKg = 80,
                    RestSeconds = 90,
                    Rpe = 8,
                    DurationMinutes = 30,
                    OrderIndex = 1
                }
            }
        };

        _mockWorkoutLogService
            .Setup(s => s.CreateWorkoutLogAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateWorkoutLog(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(WorkoutLogsController.CreateWorkoutLog));
        createdResult.Value.Should().NotBeNull();
        var json = JsonSerializer.Serialize(createdResult.Value);
        var response = JsonSerializer.Deserialize<ApiResponse<WorkoutLogResponse>>(json, _jsonOptions);
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("Workout log created successfully");
        response.Data.Should().NotBeNull();
        response.Data!.WorkoutLogId.Should().Be(1);
        response.Data.TotalDurationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task CreateWorkoutLog_ShouldReturnBadRequest_WhenModelInvalid()
    {
        // Arrange
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = new DateTime(2025, 12, 19),
            Notes = "Chest day workout",
            ExerciseSessions = new List<CreateExerciseSessionRequest>() // Empty sessions
        };

        _controller.ModelState.AddModelError("ExerciseSessions", "At least one exercise session is required");

        // Act
        var result = await _controller.CreateWorkoutLog(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkoutLogs_ShouldReturnOk_WhenRetrievalSucceeds()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 1;
        var pageSize = 20;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var workoutLogs = new List<WorkoutLogResponse>
        {
            new WorkoutLogResponse
            {
                WorkoutLogId = 1,
                UserId = 1,
                WorkoutDate = new DateTime(2025, 12, 19),
                TotalDurationMinutes = 45,
                EstimatedCaloriesBurned = 300,
                Notes = "Full body workout",
                CreatedAt = DateTime.UtcNow,
                ExerciseSessions = new List<ExerciseSessionDto>()
            }
        };

        var paginatedResult = new PaginatedResult<WorkoutLogResponse>(
            workoutLogs, 1, pageNumber, pageSize
        );

        _mockWorkoutLogService
            .Setup(s => s.GetWorkoutLogsAsync(userId, pageNumber, pageSize, startDate, endDate))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetWorkoutLogs(pageNumber, pageSize, startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be("Workout logs retrieved successfully");
        var data = root.GetProperty("data");
        data.GetProperty("Items").EnumerateArray().Should().HaveCount(1);
        data.GetProperty("TotalItems").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetWorkoutLogs_ShouldReturnBadRequest_WhenPageNumberInvalid()
    {
        // Act
        var result = await _controller.GetWorkoutLogs(pageNumber: 0);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Page number must be >= 1");
    }

    [Fact]
    public async Task GetWorkoutLogs_ShouldReturnBadRequest_WhenPageSizeInvalid()
    {
        // Act
        var result = await _controller.GetWorkoutLogs(pageSize: 150);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetWorkoutLogs_ShouldReturnBadRequest_WhenDateRangeInvalid()
    {
        // Act
        var result = await _controller.GetWorkoutLogs(
            startDate: new DateTime(2025, 12, 31),
            endDate: new DateTime(2025, 12, 1)
        );

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Start date must be before or equal to end date");
    }
}