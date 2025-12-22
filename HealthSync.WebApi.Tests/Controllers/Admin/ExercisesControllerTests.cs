using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HealthSync.WebApi.Controllers.Admin;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class ExercisesControllerTests
{
    private readonly Mock<IExerciseService> _mockExerciseService;
    private readonly Mock<ILogger<ExercisesController>> _mockLogger;
    private readonly ExercisesController _controller;

    public ExercisesControllerTests()
    {
        _mockExerciseService = new Mock<IExerciseService>();
        _mockLogger = new Mock<ILogger<ExercisesController>>();
        _controller = new ExercisesController(_mockExerciseService.Object, _mockLogger.Object);

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
    public async Task GetExerciseById_ShouldReturnOk_WhenExerciseExists()
    {
        // Arrange
        int testId = 1;
        var fakeExercise = new ExerciseDto
        {
            Id = testId,
            Name = "Push Up",
            MuscleGroup = "Chest",
            Difficulty = "Beginner"
        };

        _mockExerciseService.Setup(s => s.GetExerciseByIdAsync(testId))
                            .ReturnsAsync(fakeExercise);

        // Act
        var result = await _controller.GetExerciseById(testId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnData = Assert.IsType<ExerciseDto>(okResult.Value);
        Assert.Equal(testId, returnData.Id);
        Assert.Equal("Push Up", returnData.Name);
    }

    [Fact]
    public async Task GetExerciseById_ShouldReturnNotFound_WhenExerciseDoesNotExist()
    {
        // Arrange
        int testId = 99;
        _mockExerciseService.Setup(s => s.GetExerciseByIdAsync(testId))
                            .ReturnsAsync((ExerciseDto?)null);

        // Act
        var result = await _controller.GetExerciseById(testId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains("Exercise with ID 99 not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task GetExercises_ShouldReturnOk_WithValidParameters()
    {
        // Arrange
        var fakeResult = new PaginatedResult<ExerciseDto>
        {
            Items = new List<ExerciseDto>
            {
                new ExerciseDto { Id = 1, Name = "Push Up", MuscleGroup = "Chest", Difficulty = "Beginner" }
            },
            TotalItems = 1,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _mockExerciseService.Setup(s => s.GetExercisesAsync("Chest", "Beginner", "Bodyweight", 1, 20))
                            .ReturnsAsync(fakeResult);

        // Act
        var result = await _controller.GetExercises("Chest", "Beginner", "Bodyweight", 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnData = Assert.IsType<PaginatedResult<ExerciseDto>>(okResult.Value);
        Assert.Single(returnData.Items);
        Assert.Equal(1, returnData.TotalItems);
    }

    [Fact]
    public async Task CreateExercise_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var request = new CreateExerciseRequest
        {
            Name = "Pull Up",
            MuscleGroup = "Back",
            Difficulty = "Intermediate",
            Equipment = "Bar"
        };

        var createdExercise = new ExerciseDto
        {
            Id = 2,
            Name = "Pull Up",
            MuscleGroup = "Back",
            Difficulty = "Intermediate",
            Equipment = "Bar"
        };

        _mockExerciseService.Setup(s => s.CreateExerciseAsync(request, 1))
                            .ReturnsAsync(createdExercise);

        // Act
        var result = await _controller.CreateExercise(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnData = Assert.IsType<ExerciseDto>(createdResult.Value);
        Assert.Equal(2, returnData.Id);
        Assert.Equal("Pull Up", returnData.Name);
        Assert.Equal("GetExerciseById", createdResult.ActionName);
    }

    [Fact]
    public async Task CreateExercise_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Name is required");

        var request = new CreateExerciseRequest();

        // Act
        var result = await _controller.CreateExercise(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateExercise_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        int exerciseId = 1;
        var request = new UpdateExerciseRequest
        {
            Name = "Updated Push Up",
            Description = "Updated description"
        };

        var updatedExercise = new ExerciseDto
        {
            Id = exerciseId,
            Name = "Updated Push Up",
            MuscleGroup = "Chest",
            Difficulty = "Beginner",
            Description = "Updated description"
        };

        _mockExerciseService.Setup(s => s.UpdateExerciseAsync(exerciseId, request))
                            .ReturnsAsync(updatedExercise);

        // Act
        var result = await _controller.UpdateExercise(exerciseId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnData = Assert.IsType<ExerciseDto>(okResult.Value);
        Assert.Equal("Updated Push Up", returnData.Name);
    }

    [Fact]
    public async Task UpdateExercise_ShouldReturnNotFound_WhenExerciseDoesNotExist()
    {
        // Arrange
        int exerciseId = 99;
        var request = new UpdateExerciseRequest { Name = "Test" };

        _mockExerciseService.Setup(s => s.UpdateExerciseAsync(exerciseId, request))
                            .ThrowsAsync(new KeyNotFoundException("Exercise not found"));

        // Act
        var result = await _controller.UpdateExercise(exerciseId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains("Exercise not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteExercise_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        int exerciseId = 1;
        _mockExerciseService.Setup(s => s.DeleteExerciseAsync(exerciseId))
                            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteExercise(exerciseId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteExercise_ShouldReturnNotFound_WhenExerciseDoesNotExist()
    {
        // Arrange
        int exerciseId = 99;
        _mockExerciseService.Setup(s => s.DeleteExerciseAsync(exerciseId))
                            .ThrowsAsync(new KeyNotFoundException("Exercise not found"));

        // Act
        var result = await _controller.DeleteExercise(exerciseId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains("Exercise not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteExercise_ShouldReturnConflict_WhenExerciseInUse()
    {
        // Arrange
        int exerciseId = 1;
        _mockExerciseService.Setup(s => s.DeleteExerciseAsync(exerciseId))
                            .ThrowsAsync(new InvalidOperationException("Exercise is in use"));

        // Act
        var result = await _controller.DeleteExercise(exerciseId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
        Assert.Contains("Exercise is in use", conflictResult.Value.ToString());
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnOk_WhenValidImage()
    {
        // Arrange
        int exerciseId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        var updatedExercise = new ExerciseDto
        {
            Id = exerciseId,
            Name = "Push Up",
            MuscleGroup = "Chest",
            Difficulty = "Beginner",
            ImageUrl = "https://minio.example.com/exercises/test.jpg"
        };

        _mockExerciseService.Setup(s => s.UploadExerciseImageAsync(exerciseId, fileMock.Object))
                            .ReturnsAsync(updatedExercise);

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnData = Assert.IsType<ExerciseDto>(okResult.Value);
        Assert.Equal(exerciseId, returnData.Id);
        Assert.Equal("https://minio.example.com/exercises/test.jpg", returnData.ImageUrl);
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnBadRequest_WhenNoFile()
    {
        // Arrange
        int exerciseId = 1;
        IFormFile? file = null;

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, file!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No file uploaded", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnBadRequest_WhenFileTooLarge()
    {
        // Arrange
        int exerciseId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6MB
        fileMock.Setup(_ => _.FileName).Returns("large.jpg");

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("File size must be less than 5MB", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnBadRequest_WhenInvalidFileType()
    {
        // Arrange
        int exerciseId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.gif");

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Only JPG and PNG files are allowed", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnNotFound_WhenExerciseDoesNotExist()
    {
        // Arrange
        int exerciseId = 999;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        _mockExerciseService.Setup(s => s.UploadExerciseImageAsync(exerciseId, fileMock.Object))
                            .ThrowsAsync(new KeyNotFoundException("Exercise not found"));

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, fileMock.Object);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains("Exercise not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task GetExercises_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockExerciseService.Setup(s => s.GetExercisesAsync(null, null, null, 1, 20))
                            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetExercises(null, null, null, 1, 20);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }

    [Fact]
    public async Task GetExercises_ShouldHandleLargePageSize()
    {
        // Arrange
        var fakeResult = new PaginatedResult<ExerciseDto>
        {
            Items = new List<ExerciseDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            PageSize = 100
        };
        _mockExerciseService.Setup(s => s.GetExercisesAsync(null, null, null, 1, 100))
                            .ReturnsAsync(fakeResult);

        // Act
        var result = await _controller.GetExercises(null, null, null, 1, 150);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<PaginatedResult<ExerciseDto>>(okResult.Value);
        Assert.Equal(100, returnedResult.PageSize);
    }

    [Fact]
    public async Task GetExercises_ShouldHandleNegativePageNumber()
    {
        // Arrange
        var fakeResult = new PaginatedResult<ExerciseDto>
        {
            Items = new List<ExerciseDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            PageSize = 20
        };
        _mockExerciseService.Setup(s => s.GetExercisesAsync(null, null, null, 1, 20))
                            .ReturnsAsync(fakeResult);

        // Act
        var result = await _controller.GetExercises(null, null, null, -5, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<PaginatedResult<ExerciseDto>>(okResult.Value);
        Assert.Equal(1, returnedResult.CurrentPage);
    }

    [Fact]
    public async Task GetExerciseById_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        int testId = 1;
        _mockExerciseService.Setup(s => s.GetExerciseByIdAsync(testId))
                            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetExerciseById(testId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }

    [Fact]
    public async Task CreateExercise_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new CreateExerciseRequest
        {
            Name = "Test Exercise",
            MuscleGroup = "Chest",
            Difficulty = "Beginner"
        };

        _mockExerciseService.Setup(s => s.CreateExerciseAsync(request, 1))
                            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateExercise(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateExercise_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        int testId = 1;
        var request = new UpdateExerciseRequest
        {
            Name = "", // Invalid: empty name
            MuscleGroup = "Chest"
        };

        // Add model state error
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.UpdateExercise(testId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateExercise_ShouldReturnBadRequest_WhenInvalidOperationExceptionOccurs()
    {
        // Arrange
        int testId = 1;
        var request = new UpdateExerciseRequest
        {
            Name = "Updated Exercise",
            MuscleGroup = "Chest"
        };

        _mockExerciseService.Setup(s => s.UpdateExerciseAsync(testId, request))
                            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _controller.UpdateExercise(testId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Contains("Invalid operation", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateExercise_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        int testId = 1;
        var request = new UpdateExerciseRequest
        {
            Name = "Updated Exercise",
            MuscleGroup = "Chest"
        };

        _mockExerciseService.Setup(s => s.UpdateExerciseAsync(testId, request))
                            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateExercise(testId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }

    [Fact]
    public async Task UploadExerciseImage_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        int exerciseId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        _mockExerciseService.Setup(s => s.UploadExerciseImageAsync(exerciseId, fileMock.Object))
                            .ThrowsAsync(new Exception("Upload failed"));

        // Act
        var result = await _controller.UploadExerciseImage(exerciseId, fileMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteExercise_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        int testId = 1;
        _mockExerciseService.Setup(s => s.DeleteExerciseAsync(testId))
                            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteExercise(testId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
    }
}

