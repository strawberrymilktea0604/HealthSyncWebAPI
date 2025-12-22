using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ExerciseSessionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExerciseSessionRepository _repository;

    public ExerciseSessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ExerciseSessionRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllExerciseSessions()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest, DifficultyLevel = DifficultyLevel.Intermediate, Equipment = Equipment.Barbell };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 60 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session1 = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 4, Reps = 10, WeightKg = 80 };
        var session2 = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 3, Reps = 12, WeightKg = 60 };
        _context.ExerciseSessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.NotNull(s.Exercise));
        Assert.All(result, s => Assert.NotNull(s.WorkoutLog));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsExerciseSession()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Squat", MuscleGroup = MuscleGroup.Legs, DifficultyLevel = DifficultyLevel.Advanced, Equipment = Equipment.Barbell };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 45 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 5, Reps = 5, WeightKg = 100 };
        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(session.ExerciseSessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.ExerciseSessionId, result.ExerciseSessionId);
        Assert.Equal(5, result.Sets);
        Assert.Equal(5, result.Reps);
        Assert.NotNull(result.Exercise);
        Assert.NotNull(result.WorkoutLog);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByWorkoutIdAsync_ReturnsSessionsForWorkout()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise1 = new Exercise { Name = "Deadlift", MuscleGroup = MuscleGroup.Back, DifficultyLevel = DifficultyLevel.Advanced, Equipment = Equipment.Barbell };
        var exercise2 = new Exercise { Name = "Pull-up", MuscleGroup = MuscleGroup.Back, DifficultyLevel = DifficultyLevel.Intermediate, Equipment = Equipment.Bodyweight };
        _context.Exercises.AddRange(exercise1, exercise2);
        await _context.SaveChangesAsync();

        var workoutLog1 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 60 };
        var workoutLog2 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow.AddDays(-1), TotalDurationMinutes = 45 };
        _context.WorkoutLogs.AddRange(workoutLog1, workoutLog2);
        await _context.SaveChangesAsync();

        var session1 = new ExerciseSession { WorkoutLogId = workoutLog1.WorkoutLogId, ExerciseId = exercise1.ExerciseId, Sets = 3, Reps = 8 };
        var session2 = new ExerciseSession { WorkoutLogId = workoutLog1.WorkoutLogId, ExerciseId = exercise2.ExerciseId, Sets = 4, Reps = 10 };
        var session3 = new ExerciseSession { WorkoutLogId = workoutLog2.WorkoutLogId, ExerciseId = exercise1.ExerciseId, Sets = 5, Reps = 5 };
        _context.ExerciseSessions.AddRange(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByWorkoutIdAsync(workoutLog1.WorkoutLogId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Equal(workoutLog1.WorkoutLogId, s.WorkoutLogId));
        Assert.All(result, s => Assert.NotNull(s.Exercise));
    }

    [Fact]
    public async Task GetByExerciseIdAsync_ReturnsSessionsForExercise()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise1 = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest, DifficultyLevel = DifficultyLevel.Intermediate, Equipment = Equipment.Barbell };
        var exercise2 = new Exercise { Name = "Dumbbell Press", MuscleGroup = MuscleGroup.Chest, DifficultyLevel = DifficultyLevel.Beginner, Equipment = Equipment.Dumbbell };
        _context.Exercises.AddRange(exercise1, exercise2);
        await _context.SaveChangesAsync();

        var workoutLog1 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 60 };
        var workoutLog2 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow.AddDays(-1), TotalDurationMinutes = 45 };
        _context.WorkoutLogs.AddRange(workoutLog1, workoutLog2);
        await _context.SaveChangesAsync();

        var session1 = new ExerciseSession { WorkoutLogId = workoutLog1.WorkoutLogId, ExerciseId = exercise1.ExerciseId, Sets = 4, Reps = 10 };
        var session2 = new ExerciseSession { WorkoutLogId = workoutLog2.WorkoutLogId, ExerciseId = exercise1.ExerciseId, Sets = 3, Reps = 12 };
        var session3 = new ExerciseSession { WorkoutLogId = workoutLog1.WorkoutLogId, ExerciseId = exercise2.ExerciseId, Sets = 3, Reps = 15 };
        _context.ExerciseSessions.AddRange(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByExerciseIdAsync(exercise1.ExerciseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Equal(exercise1.ExerciseId, s.ExerciseId));
        Assert.All(result, s => Assert.NotNull(s.WorkoutLog));
    }

    [Fact]
    public async Task AddAsync_AddsExerciseSession()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Push-up", MuscleGroup = MuscleGroup.Chest, DifficultyLevel = DifficultyLevel.Beginner, Equipment = Equipment.Bodyweight };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 30 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
            ExerciseId = exercise.ExerciseId,
            Sets = 3,
            Reps = 20,
            Rpe = 7
        };

        // Act
        var result = await _repository.AddAsync(session);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ExerciseSessionId > 0);
        Assert.Equal(3, result.Sets);
        Assert.Equal(20, result.Reps);
        Assert.Equal(7, result.Rpe);

        var savedSession = await _context.ExerciseSessions.FindAsync(result.ExerciseSessionId);
        Assert.NotNull(savedSession);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExerciseSession()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Lat Pulldown", MuscleGroup = MuscleGroup.Back, DifficultyLevel = DifficultyLevel.Intermediate, Equipment = Equipment.Cable };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 50 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 3, Reps = 10, WeightKg = 50 };
        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        session.Sets = 4;
        session.Reps = 12;
        session.WeightKg = 55;
        await _repository.UpdateAsync(session);

        // Assert
        var updatedSession = await _context.ExerciseSessions.FindAsync(session.ExerciseSessionId);
        Assert.NotNull(updatedSession);
        Assert.Equal(4, updatedSession.Sets);
        Assert.Equal(12, updatedSession.Reps);
        Assert.Equal(55, updatedSession.WeightKg);
    }

    [Fact]
    public async Task DeleteAsync_DeletesExerciseSession()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Leg Press", MuscleGroup = MuscleGroup.Legs, DifficultyLevel = DifficultyLevel.Beginner, Equipment = Equipment.Machine };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 40 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 4, Reps = 15 };
        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(session.ExerciseSessionId);

        // Assert
        var deletedSession = await _context.ExerciseSessions.FindAsync(session.ExerciseSessionId);
        Assert.Null(deletedSession);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNotThrow()
    {
        // Act & Assert
        await _repository.DeleteAsync(999);
        // Should not throw exception
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsSavedChangesCount()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com", PasswordHash = "hash", Role = "Customer" };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var exercise = new Exercise { Name = "Bicep Curl", MuscleGroup = MuscleGroup.Arms, DifficultyLevel = DifficultyLevel.Beginner, Equipment = Equipment.Dumbbell };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.UtcNow, TotalDurationMinutes = 20 };
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession { WorkoutLogId = workoutLog.WorkoutLogId, ExerciseId = exercise.ExerciseId, Sets = 3, Reps = 12 };
        _context.ExerciseSessions.Add(session);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

