using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class WorkoutLogRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WorkoutLogRepository _repository;

    public WorkoutLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new WorkoutLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateWithSessionsAsync_ShouldCreateWorkoutLogWithSessions()
    {
        // Arrange
        var workoutLog = new WorkoutLog
        {
            UserId = 1,
            WorkoutDate = DateTime.Today,
            TotalDurationMinutes = 60,
            EstimatedCaloriesBurned = 300
        };

        var sessions = new List<ExerciseSession>
        {
            new ExerciseSession
            {
                ExerciseId = 1,
                Sets = 3,
                Reps = 10,
                WeightKg = 50,
                RestSeconds = 60,
                OrderIndex = 1
            },
            new ExerciseSession
            {
                ExerciseId = 2,
                Sets = 4,
                Reps = 8,
                WeightKg = 40,
                RestSeconds = 90,
                OrderIndex = 2
            }
        };

        // Act
        var result = await _repository.CreateWithSessionsAsync(workoutLog, sessions);

        // Assert
        result.WorkoutLogId.Should().BeGreaterThan(0);
        result.ExerciseSessions.Should().HaveCount(2);
        result.ExerciseSessions.All(es => es.WorkoutLogId == result.WorkoutLogId).Should().BeTrue();

        var savedWorkoutLog = await _context.WorkoutLogs
            .Include(wl => wl.ExerciseSessions)
            .FirstOrDefaultAsync(wl => wl.WorkoutLogId == result.WorkoutLogId);
        savedWorkoutLog.Should().NotBeNull();
        savedWorkoutLog!.ExerciseSessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var user1 = new ApplicationUser { Email = "user1@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        var user2 = new ApplicationUser { Email = "user2@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var workoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog { UserId = user1.UserId, WorkoutDate = DateTime.Today, TotalDurationMinutes = 30 },
            new WorkoutLog { UserId = user1.UserId, WorkoutDate = DateTime.Today.AddDays(-1), TotalDurationMinutes = 45 },
            new WorkoutLog { UserId = user1.UserId, WorkoutDate = DateTime.Today.AddDays(-2), TotalDurationMinutes = 60 },
            new WorkoutLog { UserId = user2.UserId, WorkoutDate = DateTime.Today, TotalDurationMinutes = 30 }
        };
        await _context.WorkoutLogs.AddRangeAsync(workoutLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(user1.UserId, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(3);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.All(wl => wl.UserId == user1.UserId).Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithDateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var workoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.Today.AddDays(-5), TotalDurationMinutes = 30 },
            new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.Today.AddDays(-3), TotalDurationMinutes = 45 },
            new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.Today.AddDays(-1), TotalDurationMinutes = 60 }
        };
        await _context.WorkoutLogs.AddRangeAsync(workoutLogs);
        await _context.SaveChangesAsync();

        var startDate = DateTime.Today.AddDays(-4);
        var endDate = DateTime.Today.AddDays(-2);

        // Act
        var result = await _repository.GetByUserIdAsync(user.UserId, 1, 10, startDate, endDate);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().WorkoutDate.Should().Be(DateTime.Today.AddDays(-3));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnWorkoutLogWithSessions()
    {
        // Arrange
        var workoutLog = new WorkoutLog
        {
            UserId = 1,
            WorkoutDate = DateTime.Today,
            TotalDurationMinutes = 60
        };
        await _context.WorkoutLogs.AddAsync(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
            ExerciseId = 1,
            Sets = 3,
            Reps = 10
        };
        await _context.ExerciseSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(workoutLog.WorkoutLogId);

        // Assert
        result.Should().NotBeNull();
        result!.WorkoutLogId.Should().Be(workoutLog.WorkoutLogId);
        result.ExerciseSessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddWorkoutLogToDatabase()
    {
        // Arrange
        var workoutLog = new WorkoutLog
        {
            UserId = 1,
            WorkoutDate = DateTime.Today,
            TotalDurationMinutes = 45,
            EstimatedCaloriesBurned = 250
        };

        // Act
        var result = await _repository.AddAsync(workoutLog);

        // Assert
        result.WorkoutLogId.Should().BeGreaterThan(0);
        var savedWorkoutLog = await _context.WorkoutLogs.FindAsync(result.WorkoutLogId);
        savedWorkoutLog.Should().NotBeNull();
        savedWorkoutLog!.TotalDurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task AddExerciseSessionAsync_ShouldAddSessionToDatabase()
    {
        // Arrange
        var workoutLog = new WorkoutLog { UserId = 1, WorkoutDate = DateTime.Today };
        await _context.WorkoutLogs.AddAsync(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
            ExerciseId = 1,
            Sets = 4,
            Reps = 12,
            WeightKg = 60
        };

        // Act
        var result = await _repository.AddExerciseSessionAsync(session);

        // Assert
        result.ExerciseSessionId.Should().BeGreaterThan(0);
        var savedSession = await _context.ExerciseSessions.FindAsync(result.ExerciseSessionId);
        savedSession.Should().NotBeNull();
        savedSession!.Sets.Should().Be(4);
    }

    [Fact]
    public async Task GetExerciseSessionsAsync_ShouldReturnSessionsForWorkoutLog()
    {
        // Arrange
        var workoutLog = new WorkoutLog { UserId = 1, WorkoutDate = DateTime.Today };
        await _context.WorkoutLogs.AddAsync(workoutLog);
        await _context.SaveChangesAsync();

        var sessions = new List<ExerciseSession>
        {
            new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = 1, Sets = 3 },
            new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = 2, Sets = 4 },
            new ExerciseSession { WorkoutLogId = 999, ExerciseId = 3, Sets = 5 } // Different workout log
        };
        await _context.ExerciseSessions.AddRangeAsync(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetExerciseSessionsAsync(workoutLog.WorkoutLogId);

        // Assert
        result.Should().HaveCount(2);
        result.All(es => es.WorkoutLogId == workoutLog.WorkoutLogId).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateWorkoutLogInDatabase()
    {
        // Arrange
        var workoutLog = new WorkoutLog
        {
            UserId = 1,
            WorkoutDate = DateTime.Today,
            TotalDurationMinutes = 30
        };
        await _context.WorkoutLogs.AddAsync(workoutLog);
        await _context.SaveChangesAsync();

        // Act
        workoutLog.TotalDurationMinutes = 60;
        await _repository.UpdateAsync(workoutLog);

        // Assert
        var updatedWorkoutLog = await _context.WorkoutLogs.FindAsync(workoutLog.WorkoutLogId);
        updatedWorkoutLog.Should().NotBeNull();
        updatedWorkoutLog!.TotalDurationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var currentMonth = DateTime.Today;
        var lastMonth = DateTime.Today.AddMonths(-1);

        var workoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog { UserId = user.UserId, WorkoutDate = currentMonth, TotalDurationMinutes = 30 },
            new WorkoutLog { UserId = user.UserId, WorkoutDate = currentMonth.AddDays(-5), TotalDurationMinutes = 45 },
            new WorkoutLog { UserId = user.UserId, WorkoutDate = lastMonth, TotalDurationMinutes = 60 },
            new WorkoutLog { UserId = 999, WorkoutDate = currentMonth, TotalDurationMinutes = 30 } // Different user
        };
        await _context.WorkoutLogs.AddRangeAsync(workoutLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(user.UserId, currentMonth.Year, currentMonth.Month);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountWorkoutLogsTodayAsync_ShouldReturnTodaysWorkoutLogsCount()
    {
        // Arrange
        var today = DateTime.Today;
        var yesterday = DateTime.Today.AddDays(-1);

        var workoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog { UserId = 1, WorkoutDate = today, TotalDurationMinutes = 30 },
            new WorkoutLog { UserId = 2, WorkoutDate = today, TotalDurationMinutes = 45 },
            new WorkoutLog { UserId = 3, WorkoutDate = yesterday, TotalDurationMinutes = 60 }
        };
        await _context.WorkoutLogs.AddRangeAsync(workoutLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountWorkoutLogsTodayAsync();

        // Assert
        result.Should().Be(2);
    }
}

