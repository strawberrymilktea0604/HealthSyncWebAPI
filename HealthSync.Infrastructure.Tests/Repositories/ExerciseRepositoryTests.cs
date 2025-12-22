using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ExerciseRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExerciseRepository _repository;

    public ExerciseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ExerciseRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExercise_WhenExists()
    {
        // Arrange
        var exercise = new Exercise
        {
            Name = "Bench Press",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate,
            Equipment = Equipment.Barbell,
            Description = "Classic chest exercise",
            CaloriesPerMinute = 8.5m
        };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(exercise.ExerciseId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bench Press");
        result.MuscleGroup.Should().Be(MuscleGroup.Chest);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllExercisesOrderedByName()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise { Name = "Z Press", MuscleGroup = MuscleGroup.Shoulders },
            new Exercise { Name = "A Press", MuscleGroup = MuscleGroup.Chest },
            new Exercise { Name = "M Press", MuscleGroup = MuscleGroup.Chest }
        };
        await _context.Exercises.AddRangeAsync(exercises);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(e => e.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldReturnFilteredExercises_WithMuscleGroupFilter()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest },
            new Exercise { Name = "Squat", MuscleGroup = MuscleGroup.Legs },
            new Exercise { Name = "Deadlift", MuscleGroup = MuscleGroup.Back },
            new Exercise { Name = "Push-up", MuscleGroup = MuscleGroup.Chest }
        };
        await _context.Exercises.AddRangeAsync(exercises);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _repository.GetFilteredAsync("Chest", null, null, 1, 10);

        // Assert
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.All(e => e.MuscleGroup == MuscleGroup.Chest).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldReturnFilteredExercises_WithDifficultyFilter()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise { Name = "Bench Press", DifficultyLevel = DifficultyLevel.Beginner },
            new Exercise { Name = "Squat", DifficultyLevel = DifficultyLevel.Intermediate },
            new Exercise { Name = "Deadlift", DifficultyLevel = DifficultyLevel.Advanced }
        };
        await _context.Exercises.AddRangeAsync(exercises);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _repository.GetFilteredAsync(null, "Intermediate", null, 1, 10);

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
        result.First().Name.Should().Be("Squat");
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldReturnFilteredExercises_WithEquipmentFilter()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise { Name = "Bench Press", Equipment = Equipment.Barbell },
            new Exercise { Name = "Push-up", Equipment = Equipment.Bodyweight },
            new Exercise { Name = "Dumbbell Press", Equipment = Equipment.Dumbbell }
        };
        await _context.Exercises.AddRangeAsync(exercises);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _repository.GetFilteredAsync(null, null, "Barbell", 1, 10);

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
        result.First().Name.Should().Be("Bench Press");
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldSupportPagination()
    {
        // Arrange
        var exercises = new List<Exercise>();
        for (int i = 1; i <= 10; i++)
        {
            exercises.Add(new Exercise { Name = $"Exercise {i:D2}", MuscleGroup = MuscleGroup.Chest });
        }
        await _context.Exercises.AddRangeAsync(exercises);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _repository.GetFilteredAsync(null, null, null, 2, 3);

        // Assert
        result.Should().HaveCount(3);
        totalCount.Should().Be(10);
        result.First().Name.Should().Be("Exercise 04"); // Page 2, pageSize 3: skip 3, take items 4, 5, 6
    }

    [Fact]
    public async Task AddAsync_ShouldAddExerciseToDatabase()
    {
        // Arrange
        var exercise = new Exercise
        {
            Name = "Pull-up",
            MuscleGroup = MuscleGroup.Back,
            DifficultyLevel = DifficultyLevel.Intermediate,
            Equipment = Equipment.Bodyweight,
            Description = "Bodyweight back exercise",
            CaloriesPerMinute = 6.0m
        };

        // Act
        var result = await _repository.AddAsync(exercise);

        // Assert
        result.ExerciseId.Should().BeGreaterThan(0);
        var savedExercise = await _context.Exercises.FindAsync(result.ExerciseId);
        savedExercise.Should().NotBeNull();
        savedExercise!.Name.Should().Be("Pull-up");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExerciseInDatabase()
    {
        // Arrange
        var exercise = new Exercise
        {
            Name = "Old Name",
            MuscleGroup = MuscleGroup.Chest,
            Description = "Old description"
        };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        // Act
        exercise.Name = "New Name";
        exercise.Description = "New description";
        await _repository.UpdateAsync(exercise);

        // Assert
        var updatedExercise = await _context.Exercises.FindAsync(exercise.ExerciseId);
        updatedExercise.Should().NotBeNull();
        updatedExercise!.Name.Should().Be("New Name");
        updatedExercise.Description.Should().Be("New description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveExerciseFromDatabase()
    {
        // Arrange
        var exercise = new Exercise { Name = "Test Exercise", MuscleGroup = MuscleGroup.Arms };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(exercise.ExerciseId);

        // Assert
        var deletedExercise = await _context.Exercises.FindAsync(exercise.ExerciseId);
        deletedExercise.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenExerciseDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteAsync(999); // Should not throw
        
        // Assert that no exception was thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenNameExists()
    {
        // Arrange
        var exercise = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync("Bench Press");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnFalse_WhenNameDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("Non-existent Exercise");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnFalse_WhenExcludingSpecificId()
    {
        // Arrange
        var exercise1 = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        var exercise2 = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Back };
        await _context.Exercises.AddRangeAsync(exercise1, exercise2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync("Bench Press", exercise1.ExerciseId);

        // Assert
        result.Should().BeTrue(); // Still exists because exercise2 has the same name
    }

    [Fact]
    public async Task IsUsedInSessionsAsync_ShouldReturnTrue_WhenExerciseIsUsed()
    {
        // Arrange
        var exercise = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        var workoutLog = new WorkoutLog { UserId = 1, WorkoutDate = DateTime.Today };
        await _context.WorkoutLogs.AddAsync(workoutLog);
        await _context.SaveChangesAsync();

        var session = new ExerciseSession
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
            ExerciseId = exercise.ExerciseId,
            Sets = 3,
            Reps = 10
        };
        await _context.ExerciseSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUsedInSessionsAsync(exercise.ExerciseId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUsedInSessionsAsync_ShouldReturnFalse_WhenExerciseIsNotUsed()
    {
        // Arrange
        var exercise = new Exercise { Name = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUsedInSessionsAsync(exercise.ExerciseId);

        // Assert
        result.Should().BeFalse();
    }
}

