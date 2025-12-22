using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using FluentAssertions;

namespace HealthSync.Application.Tests.Services;

public class ExerciseServiceTests
{
    private readonly Mock<IExerciseRepository> _exerciseRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly ExerciseService _service;

    public ExerciseServiceTests()
    {
        _exerciseRepositoryMock = new Mock<IExerciseRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        _service = new ExerciseService(
            _exerciseRepositoryMock.Object,
            _fileStorageServiceMock.Object);
    }

    [Fact]
    public async Task GetAllExercisesAsync_ShouldReturnAllExercisesAsDtos()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise
            {
                ExerciseId = 1,
                Name = "Bench Press",
                MuscleGroup = MuscleGroup.Chest,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Equipment = Equipment.Barbell,
                Description = "Classic chest exercise",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Exercise
            {
                ExerciseId = 2,
                Name = "Squat",
                MuscleGroup = MuscleGroup.Legs,
                DifficultyLevel = DifficultyLevel.Advanced,
                Equipment = Equipment.Barbell,
                Description = "Lower body exercise",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(exercises);

        // Act
        var result = await _service.GetAllExercisesAsync();

        // Assert
        result.Should().HaveCount(2);
        var exerciseDtos = result.ToList();
        exerciseDtos[0].Id.Should().Be(1);
        exerciseDtos[0].Name.Should().Be("Bench Press");
        exerciseDtos[0].MuscleGroup.Should().Be("Chest");
        exerciseDtos[0].Difficulty.Should().Be("Intermediate");
        exerciseDtos[0].Equipment.Should().Be("Barbell");
        exerciseDtos[1].Id.Should().Be(2);
        exerciseDtos[1].Name.Should().Be("Squat");
    }

    [Fact]
    public async Task GetExercisesAsync_ShouldReturnPaginatedFilteredResults()
    {
        // Arrange
        var exercises = new List<Exercise>
        {
            new Exercise
            {
                ExerciseId = 1,
                Name = "Bench Press",
                MuscleGroup = MuscleGroup.Chest,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Equipment = Equipment.Barbell,
                Description = "Classic chest exercise",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetFilteredAsync("Chest", "Intermediate", "Barbell", 1, 10))
            .ReturnsAsync((exercises, 1));

        // Act
        var result = await _service.GetExercisesAsync("Chest", "Intermediate", "Barbell", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.First().Name.Should().Be("Bench Press");
    }

    [Fact]
    public async Task GetExercisesAsync_ShouldHandleInvalidPaginationParameters()
    {
        // Arrange
        var exercises = new List<Exercise>();

        _exerciseRepositoryMock
            .Setup(r => r.GetFilteredAsync(null, null, null, 1, 10))
            .ReturnsAsync((exercises, 0));

        // Act
        var result = await _service.GetExercisesAsync(null, null, null, 0, 100);

        // Assert
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10); // Should be clamped to 10
    }

    [Fact]
    public async Task GetExerciseByIdAsync_ShouldReturnExerciseDto_WhenExerciseExists()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Bench Press",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate,
            Equipment = Equipment.Barbell,
            Description = "Classic chest exercise",
            Instructions = "Lie on bench...",
            ImageUrl = "https://example.com/image.jpg",
            VideoUrl = "https://example.com/video.mp4",
            CaloriesPerMinute = 8.5m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        // Act
        var result = await _service.GetExerciseByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Bench Press");
        result.MuscleGroup.Should().Be("Chest");
        result.Difficulty.Should().Be("Intermediate");
        result.Equipment.Should().Be("Barbell");
        result.Description.Should().Be("Classic chest exercise");
        result.Instructions.Should().Be("Lie on bench...");
        result.ImageUrl.Should().Be("https://example.com/image.jpg");
        result.VideoUrl.Should().Be("https://example.com/video.mp4");
        result.CaloriesPerMinute.Should().Be(8.5m);
    }

    [Fact]
    public async Task GetExerciseByIdAsync_ShouldReturnNull_WhenExerciseNotFound()
    {
        // Arrange
        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Exercise?)null);

        // Act
        var result = await _service.GetExerciseByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateExerciseAsync_ShouldCreateExerciseAndReturnDto()
    {
        // Arrange
        var request = new CreateExerciseRequest
        {
            Name = "Bench Press",
            MuscleGroup = "Chest",
            Difficulty = "Intermediate",
            Equipment = "Barbell",
            Description = "Classic chest exercise",
            ImageUrl = "https://example.com/image.jpg"
        };

        var createdExercise = new Exercise
        {
            ExerciseId = 1,
            Name = request.Name,
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate,
            Equipment = Equipment.Barbell,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedByAdminId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _exerciseRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, null))
            .ReturnsAsync(false);

        _exerciseRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Exercise>()))
            .ReturnsAsync(createdExercise);

        // Act
        var result = await _service.CreateExerciseAsync(request, 1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Bench Press");
        result.MuscleGroup.Should().Be("Chest");
        result.Difficulty.Should().Be("Intermediate");
        result.Equipment.Should().Be("Barbell");
        result.Description.Should().Be("Classic chest exercise");
        result.ImageUrl.Should().Be("https://example.com/image.jpg");

        _exerciseRepositoryMock.Verify(r => r.AddAsync(It.Is<Exercise>(e =>
            e.Name == request.Name &&
            e.MuscleGroup == MuscleGroup.Chest &&
            e.DifficultyLevel == DifficultyLevel.Intermediate &&
            e.Equipment == Equipment.Barbell &&
            e.CreatedByAdminId == 1)), Times.Once);
    }

    [Fact]
    public async Task CreateExerciseAsync_ShouldThrowInvalidOperationException_WhenNameExists()
    {
        // Arrange
        var request = new CreateExerciseRequest
        {
            Name = "Bench Press",
            MuscleGroup = "Chest",
            Difficulty = "Intermediate"
        };

        _exerciseRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, null))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateExerciseAsync(request, 1));
    }

    [Fact]
    public async Task UpdateExerciseAsync_ShouldUpdateExerciseAndReturnDto()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Old Name",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Beginner,
            Equipment = Equipment.Bodyweight,
            Description = "Old description",
            ImageUrl = "old-url.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateExerciseRequest
        {
            Name = "New Name",
            MuscleGroup = "Back",
            Difficulty = "Advanced",
            Equipment = "Barbell",
            Description = "New description",
            ImageUrl = "new-url.jpg"
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _exerciseRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, 1))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateExerciseAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("New Name");
        result.MuscleGroup.Should().Be("Back");
        result.Difficulty.Should().Be("Advanced");
        result.Equipment.Should().Be("Barbell");
        result.Description.Should().Be("New description");
        result.ImageUrl.Should().Be("new-url.jpg");

        _exerciseRepositoryMock.Verify(r => r.UpdateAsync(exercise), Times.Once);
    }

    [Fact]
    public async Task UpdateExerciseAsync_ShouldThrowKeyNotFoundException_WhenExerciseNotFound()
    {
        // Arrange
        var request = new UpdateExerciseRequest { Name = "New Name" };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Exercise?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateExerciseAsync(999, request));
    }

    [Fact]
    public async Task UpdateExerciseAsync_ShouldThrowInvalidOperationException_WhenNameExists()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Old Name",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Beginner
        };

        var request = new UpdateExerciseRequest { Name = "Existing Name" };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _exerciseRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, 1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateExerciseAsync(1, request));
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ShouldUploadImageAndUpdateExercise()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Bench Press",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate,
            Description = "Classic chest exercise",
            Instructions = "Lie on bench...",
            VideoUrl = "https://example.com/video.mp4",
            CaloriesPerMinute = 8.5m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockFile = new Mock<IFormFile>();
        var uploadedUrl = "https://storage.example.com/exercises/image.jpg";

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _fileStorageServiceMock
            .Setup(s => s.UploadAsync(mockFile.Object, "exercises"))
            .ReturnsAsync(uploadedUrl);

        // Act
        var result = await _service.UploadExerciseImageAsync(1, mockFile.Object);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.ImageUrl.Should().Be(uploadedUrl);

        exercise.ImageUrl.Should().Be(uploadedUrl);
        _exerciseRepositoryMock.Verify(r => r.UpdateAsync(exercise), Times.Once);
        _fileStorageServiceMock.Verify(s => s.UploadAsync(mockFile.Object, "exercises"), Times.Once);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ShouldThrowKeyNotFoundException_WhenExerciseNotFound()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Exercise?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UploadExerciseImageAsync(999, mockFile.Object));
    }

    [Fact]
    public async Task DeleteExerciseAsync_ShouldDeleteExercise_WhenNotUsedInSessions()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Bench Press",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _exerciseRepositoryMock
            .Setup(r => r.IsUsedInSessionsAsync(1))
            .ReturnsAsync(false);

        // Act
        await _service.DeleteExerciseAsync(1);

        // Assert
        _exerciseRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteExerciseAsync_ShouldThrowKeyNotFoundException_WhenExerciseNotFound()
    {
        // Arrange
        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Exercise?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteExerciseAsync(999));
    }

    [Fact]
    public async Task DeleteExerciseAsync_ShouldThrowInvalidOperationException_WhenUsedInSessions()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = 1,
            Name = "Bench Press",
            MuscleGroup = MuscleGroup.Chest,
            DifficultyLevel = DifficultyLevel.Intermediate
        };

        _exerciseRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(exercise);

        _exerciseRepositoryMock
            .Setup(r => r.IsUsedInSessionsAsync(1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteExerciseAsync(1));
    }
}

