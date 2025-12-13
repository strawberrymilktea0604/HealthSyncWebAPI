using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class WorkoutLogServiceTests
{
    private readonly Mock<IWorkoutLogRepository> _workoutLogRepositoryMock;
    private readonly Mock<IExerciseRepository> _exerciseRepositoryMock;
    private readonly Mock<ILogger<WorkoutLogService>> _loggerMock;
    private readonly WorkoutLogService _service;

    public WorkoutLogServiceTests()
    {
        _workoutLogRepositoryMock = new Mock<IWorkoutLogRepository>();
        _exerciseRepositoryMock = new Mock<IExerciseRepository>();
        _loggerMock = new Mock<ILogger<WorkoutLogService>>();

        _service = new WorkoutLogService(
            _workoutLogRepositoryMock.Object,
            _exerciseRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldCreateWorkoutLogWithCalculatedTotals_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            Notes = "Chest day",
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 1,
                    Sets = 4,
                    Reps = 10,
                    WeightKg = 60,
                    RestSeconds = 90,
                    Rpe = 8,
                    OrderIndex = 1
                }
            }
        };

        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Bench Press",
            CaloriesPerMinute = 8.5m
        };

        var workoutLog = new WorkoutLog
        {
            WorkoutLogId = 1,
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            Notes = request.Notes
        };

        var exerciseSession = new ExerciseSession
        {
            ExerciseSessionId = 1,
            WorkoutLogId = 1,
            ExerciseId = 1,
            Sets = 4,
            Reps = 10,
            WeightKg = 60,
            RestSeconds = 90,
            Rpe = 8,
            OrderIndex = 1
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _workoutLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WorkoutLog>()))
            .ReturnsAsync(workoutLog);

        _workoutLogRepositoryMock
            .Setup(r => r.AddExerciseSessionAsync(It.IsAny<ExerciseSession>()))
            .ReturnsAsync(exerciseSession);

        _workoutLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(workoutLog);

        _workoutLogRepositoryMock
            .Setup(r => r.GetExerciseSessionsAsync(1))
            .ReturnsAsync(new List<ExerciseSession> { exerciseSession });

        _workoutLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<WorkoutLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateWorkoutLogAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.WorkoutDate.Should().Be(request.WorkoutDate);
        result.Notes.Should().Be("Chest day");
        result.ExerciseSessions.Should().HaveCount(1);

        // Verify totals were calculated
        _workoutLogRepositoryMock.Verify(r => r.UpdateAsync(It.Is<WorkoutLog>(wl =>
            wl.TotalDurationMinutes == 60 && // 4 * 10 * 90 / 60 = 60 minutes
            wl.EstimatedCaloriesBurned == 510 // 8.5 * 60 = 510 calories
        )), Times.Once);
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldThrowArgumentException_WhenWorkoutDateIsFuture()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date.AddDays(1), // Future date
            ExerciseSessions = new List<CreateExerciseSessionRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateWorkoutLogAsync(userId, request));
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldThrowArgumentException_WhenExerciseNotFound()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 999, // Non-existent exercise
                    Sets = 3,
                    Reps = 12
                }
            }
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Exercise?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateWorkoutLogAsync(userId, request));
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldThrowArgumentException_WhenSetsIsZero()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 1,
                    Sets = 0, // Invalid
                    Reps = 10
                }
            }
        };

        var exercise = new Exercise { ExerciseId = 1, Name = "Test Exercise" };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateWorkoutLogAsync(userId, request));
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldThrowArgumentException_WhenRepsIsZero()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 1,
                    Sets = 3,
                    Reps = 0 // Invalid
                }
            }
        };

        var exercise = new Exercise { ExerciseId = 1, Name = "Test Exercise" };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateWorkoutLogAsync(userId, request));
    }

    [Fact]
    public async Task CreateWorkoutLogAsync_ShouldThrowArgumentException_WhenRpeIsInvalid()
    {
        // Arrange
        var userId = 1;
        var request = new CreateWorkoutLogRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            ExerciseSessions = new List<CreateExerciseSessionRequest>
            {
                new CreateExerciseSessionRequest
                {
                    ExerciseId = 1,
                    Sets = 3,
                    Reps = 10,
                    Rpe = 15 // Invalid (should be 1-10)
                }
            }
        };

        var exercise = new Exercise { ExerciseId = 1, Name = "Test Exercise" };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateWorkoutLogAsync(userId, request));
    }

    [Fact]
    public async Task AddExerciseSessionAsync_ShouldAddSessionAndRecalculateTotals_WhenValidRequest()
    {
        // Arrange
        var workoutLogId = 1;
        var request = new CreateExerciseSessionRequest
        {
            ExerciseId = 2,
            Sets = 3,
            Reps = 15,
            WeightKg = 50,
            RestSeconds = 60,
            Rpe = 7,
            DurationMinutes = 10,
            OrderIndex = 2
        };

        var workoutLog = new WorkoutLog
        {
            WorkoutLogId = workoutLogId,
            UserId = 1,
            TotalDurationMinutes = 60,
            EstimatedCaloriesBurned = 510
        };

        var exercise = new Exercise
        {
            ExerciseId = 2,
            Name = "Squat",
            CaloriesPerMinute = 10
        };

        var exerciseSession = new ExerciseSession
        {
            ExerciseSessionId = 2,
            WorkoutLogId = workoutLogId,
            ExerciseId = 2,
            Sets = 3,
            Reps = 15,
            WeightKg = 50,
            RestSeconds = 60,
            Rpe = 7,
            DurationMinutes = 10,
            OrderIndex = 2
        };

        _workoutLogRepositoryMock
            .Setup(r => r.GetByIdAsync(workoutLogId))
            .ReturnsAsync(workoutLog);

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(exercise);

        _workoutLogRepositoryMock
            .Setup(r => r.AddExerciseSessionAsync(It.IsAny<ExerciseSession>()))
            .ReturnsAsync(exerciseSession);

        _workoutLogRepositoryMock
            .Setup(r => r.GetExerciseSessionsAsync(workoutLogId))
            .ReturnsAsync(new List<ExerciseSession> { exerciseSession });

        _workoutLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<WorkoutLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddExerciseSessionAsync(workoutLogId, request);

        // Assert
        result.Should().NotBeNull();
        result.ExerciseId.Should().Be(2);
        result.Sets.Should().Be(3);
        result.Reps.Should().Be(15);
        result.Rpe.Should().Be(7);

        // Verify totals were recalculated
        _workoutLogRepositoryMock.Verify(r => r.UpdateAsync(It.Is<WorkoutLog>(wl =>
            wl.TotalDurationMinutes == 10 && // Direct duration
            wl.EstimatedCaloriesBurned == 100 // 10 * 10 = 100 calories
        )), Times.Once);
    }

    [Fact]
    public async Task AddExerciseSessionAsync_ShouldThrowArgumentException_WhenWorkoutLogNotFound()
    {
        // Arrange
        var workoutLogId = 999;
        var request = new CreateExerciseSessionRequest
        {
            ExerciseId = 1,
            Sets = 3,
            Reps = 10
        };

        _workoutLogRepositoryMock
            .Setup(r => r.GetByIdAsync(workoutLogId))
            .ReturnsAsync((WorkoutLog?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddExerciseSessionAsync(workoutLogId, request));
    }

    [Fact]
    public async Task AddExerciseSessionAsync_ShouldThrowArgumentException_WhenWeightIsNegative()
    {
        // Arrange
        var workoutLogId = 1;
        var request = new CreateExerciseSessionRequest
        {
            ExerciseId = 1,
            Sets = 3,
            Reps = 10,
            WeightKg = -5 // Invalid
        };

        var workoutLog = new WorkoutLog { WorkoutLogId = workoutLogId };
        var exercise = new Exercise { ExerciseId = 1, Name = "Test" };

        _workoutLogRepositoryMock
            .Setup(r => r.GetByIdAsync(workoutLogId))
            .ReturnsAsync(workoutLog);

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddExerciseSessionAsync(workoutLogId, request));
    }

    [Fact]
    public async Task GetWorkoutLogsAsync_ShouldReturnPaginatedResult_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 1;
        var pageSize = 10;
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var workoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog
            {
                WorkoutLogId = 1,
                UserId = userId,
                WorkoutDate = DateTime.UtcNow.Date,
                TotalDurationMinutes = 60,
                EstimatedCaloriesBurned = 300,
                Notes = "Test workout",
                CreatedAt = DateTime.UtcNow,
                ExerciseSessions = new List<ExerciseSession>()
            }
        };

        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, pageNumber, pageSize, startDate, endDate))
            .ReturnsAsync(new PaginatedResult<WorkoutLog>
            {
                Items = workoutLogs,
                TotalItems = 1,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = 1,
                HasNext = false,
                HasPrevious = false
            });

        // Act
        var result = await _service.GetWorkoutLogsAsync(userId, pageNumber, pageSize, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.CurrentPage.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.Items.First().WorkoutLogId.Should().Be(1);
    }

    [Fact]
    public async Task GetWorkoutLogsAsync_ShouldReturnEmptyResult_WhenNoWorkoutLogs()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 1;
        var pageSize = 10;

        _workoutLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, pageNumber, pageSize, null, null))
            .ReturnsAsync(new PaginatedResult<WorkoutLog>
            {
                Items = new List<WorkoutLog>(),
                TotalItems = 0,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = false
            });

        // Act
        var result = await _service.GetWorkoutLogsAsync(userId, pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.CurrentPage.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }
}