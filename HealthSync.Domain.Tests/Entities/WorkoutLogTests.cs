using FluentAssertions;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class WorkoutLogTests
{
    [Fact]
    public void WorkoutLog_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var workoutLog = new WorkoutLog();

        // Assert
        workoutLog.WorkoutLogId.Should().Be(0);
        workoutLog.UserId.Should().Be(0);
        workoutLog.WorkoutDate.Should().Be(default(DateTime));
        workoutLog.TotalDurationMinutes.Should().Be(0);
        workoutLog.EstimatedCaloriesBurned.Should().Be(0);
        workoutLog.Notes.Should().BeNull();
        workoutLog.CreatedAt.Should().Be(default(DateTime));
        workoutLog.ExerciseSessions.Should().NotBeNull();
        workoutLog.ExerciseSessions.Should().BeEmpty();
    }

    [Fact]
    public void WorkoutLog_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var workoutLog = new WorkoutLog();
        var workoutDate = DateTime.UtcNow.Date;
        var createdAt = DateTime.UtcNow;

        // Act
        workoutLog.WorkoutLogId = 1;
        workoutLog.UserId = 123;
        workoutLog.WorkoutDate = workoutDate;
        workoutLog.TotalDurationMinutes = 60;
        workoutLog.EstimatedCaloriesBurned = 300.5m;
        workoutLog.Notes = "Chest day";
        workoutLog.CreatedAt = createdAt;

        // Assert
        workoutLog.WorkoutLogId.Should().Be(1);
        workoutLog.UserId.Should().Be(123);
        workoutLog.WorkoutDate.Should().Be(workoutDate);
        workoutLog.TotalDurationMinutes.Should().Be(60);
        workoutLog.EstimatedCaloriesBurned.Should().Be(300.5m);
        workoutLog.Notes.Should().Be("Chest day");
        workoutLog.CreatedAt.Should().Be(createdAt);
    }
}