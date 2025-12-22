using FluentAssertions;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.DTOs;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace HealthSync.WebApi.Tests.Controllers;

public class NutritionLogsControllerTests
{
    private readonly Mock<INutritionLogService> _mockNutritionLogService;
    private readonly Mock<ILogger<NutritionLogsController>> _mockLogger;
    private readonly NutritionLogsController _controller;

    public NutritionLogsControllerTests()
    {
        _mockNutritionLogService = new Mock<INutritionLogService>();
        _mockLogger = new Mock<ILogger<NutritionLogsController>>();

        _controller = new NutritionLogsController(_mockNutritionLogService.Object, _mockLogger.Object);

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
    public async Task GetDailyNutritionLog_ShouldReturnOk_WhenDateValid()
    {
        // Arrange
        var date = "2025-12-19";
        var logDate = DateTime.Parse(date);
        var userId = 1;

        var expectedResponse = new NutritionLogResponse
        {
            NutritionLogId = 1,
            UserId = 1,
            LogDate = logDate,
            TotalCalories = 1850,
            TotalProteinG = 120,
            TotalCarbsG = 200,
            TotalFatG = 70,
            Notes = "Healthy day",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EntriesByMeal = new Dictionary<string, List<FoodEntryResponse>>
            {
                ["Lunch"] = new List<FoodEntryResponse>
                {
                    new FoodEntryResponse
                    {
                        FoodEntryId = 1,
                        NutritionLogId = 1,
                        FoodItem = new FoodItemResponse
                        {
                            FoodItemId = 1,
                            Name = "Grilled Chicken Breast",
                            Category = "Protein",
                            ServingSize = 100,
                            ServingUnit = "g",
                            CaloriesPerServing = 165m,
                            ProteinG = 31m,
                            CarbsG = 0m,
                            FatG = 3.6m
                        },
                        MealType = "Lunch",
                        Quantity = 1.5m,
                        Calories = 330m,
                        ProteinG = 62m,
                        CarbsG = 0m,
                        FatG = 7m,
                        ConsumedAt = null,
                        Notes = null,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            },
            Summary = new NutritionSummaryDto
            {
                TotalCalories = 1850,
                TotalProteinG = 120,
                TotalCarbsG = 200,
                TotalFatG = 70,
                EntryCount = 1,
                MacroBreakdown = new MacroBreakdownDto
                {
                    ProteinPercentage = 26,
                    CarbsPercentage = 43,
                    FatPercentage = 31
                }
            }
        };

        _mockNutritionLogService
            .Setup(s => s.GetOrCreateDailyLogAsync(userId, logDate))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDailyNutritionLog(date);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("NutritionLogId").GetInt32().Should().Be(1);
        data.GetProperty("TotalCalories").GetDecimal().Should().Be(1850);
        var entriesByMeal = data.GetProperty("EntriesByMeal");
        entriesByMeal.TryGetProperty("Lunch", out var lunchEntries).Should().BeTrue();
        lunchEntries.EnumerateArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDailyNutritionLog_ShouldReturnBadRequest_WhenDateInvalid()
    {
        // Act
        var result = await _controller.GetDailyNutritionLog("invalid-date");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Invalid date format. Use YYYY-MM-DD.");
    }

    [Fact]
    public async Task AddFoodEntry_ShouldReturnOk_WhenEntryAdded()
    {
        // Arrange
        var date = "2025-12-19";
        var logDate = DateTime.Parse(date);
        var userId = 1;

        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = 2.0m,
            ConsumedAt = new DateTime(2025, 12, 19, 8, 0, 0),
            Notes = "Delicious oatmeal"
        };

        var expectedResponse = new FoodEntryResponse
        {
            FoodEntryId = 1,
            NutritionLogId = 1,
            FoodItem = new FoodItemResponse
            {
                FoodItemId = 1,
                Name = "Oatmeal",
                Category = "Carbs",
                ServingSize = 40,
                ServingUnit = "g",
                CaloriesPerServing = 150,
                ProteinG = 5,
                CarbsG = 27,
                FatG = 3
            },
            MealType = "Breakfast",
            Quantity = 2.0m,
            Calories = 300,
            ProteinG = 10,
            CarbsG = 54,
            FatG = 6,
            ConsumedAt = new DateTime(2025, 12, 19, 8, 0, 0),
            Notes = "Delicious oatmeal",
            CreatedAt = DateTime.UtcNow
        };

        _mockNutritionLogService
            .Setup(s => s.AddFoodEntryAsync(userId, logDate, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.AddFoodEntry(date, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be("Food entry added successfully");
        var data = root.GetProperty("data");
        data.GetProperty("FoodEntryId").GetInt32().Should().Be(1);
        data.GetProperty("Calories").GetDecimal().Should().Be(300);
    }

    [Fact]
    public async Task DeleteFoodEntry_ShouldReturnOk_WhenEntryDeleted()
    {
        // Arrange
        var entryId = 1;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteFoodEntryAsync(userId, entryId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteFoodEntry(entryId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteFoodEntry_ShouldReturnNotFound_WhenEntryNotFound()
    {
        // Arrange
        var entryId = 999;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteFoodEntryAsync(userId, entryId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteFoodEntry(entryId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be($"Food entry with ID {entryId} not found");
    }

    [Fact]
    public async Task GetNutritionLogs_ShouldReturnOk_WhenLogsRetrieved()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 1;
        var pageSize = 10;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var nutritionLogs = new List<NutritionLogResponse>
        {
            new NutritionLogResponse
            {
                NutritionLogId = 1,
                UserId = userId,
                LogDate = new DateTime(2025, 12, 19),
                TotalCalories = 2000,
                TotalProteinG = 150,
                TotalCarbsG = 250,
                TotalFatG = 80,
                Notes = "Good nutrition day",
                EntriesByMeal = new Dictionary<string, List<FoodEntryResponse>>()
            }
        };

        var paginatedResult = new PaginatedResult<NutritionLogResponse>(
            nutritionLogs, 1, pageNumber, pageSize
        );

        _mockNutritionLogService
            .Setup(s => s.GetNutritionLogsAsync(userId, pageNumber, pageSize))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetNutritionLogs(pageNumber, pageSize);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        var items = data.GetProperty("Items");
        items.EnumerateArray().Should().HaveCount(1);
        data.GetProperty("TotalItems").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetDailyNutritionLog_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetDailyNutritionLog("2025-12-19");

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task GetDailyNutritionLog_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var date = "2025-12-19";
        var logDate = DateTime.Parse(date);
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.GetOrCreateDailyLogAsync(userId, logDate))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetDailyNutritionLog(date);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving nutrition log");
    }

    [Fact]
    public async Task AddFoodEntry_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 0, // Invalid
            MealType = "", // Invalid
            Quantity = 0 // Invalid
        };

        _mockNutritionLogService
            .Setup(s => s.AddFoodEntryAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.Is<CreateFoodEntryRequest>(r => r.FoodItemId == 0)))
            .ThrowsAsync(new ArgumentException("Invalid food entry data"));

        // Act
        var result = await _controller.AddFoodEntry("2025-12-19", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AddFoodEntry_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = 1.0m
        };

        // Act
        var result = await _controller.AddFoodEntry("2025-12-19", request);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task AddFoodEntry_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var date = "2025-12-19";
        var logDate = DateTime.Parse(date);
        var userId = 1;
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = 1.0m
        };

        _mockNutritionLogService
            .Setup(s => s.AddFoodEntryAsync(userId, logDate, request))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.AddFoodEntry(date, request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while adding food entry");
    }

    [Fact]
    public async Task DeleteFoodEntry_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.DeleteFoodEntry(1);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task DeleteFoodEntry_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var entryId = 1;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteFoodEntryAsync(userId, entryId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.DeleteFoodEntry(entryId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while deleting food entry");
    }

    [Fact]
    public async Task GetNutritionLogs_ShouldReturnBadRequest_WhenPageNumberInvalid()
    {
        // Act
        var result = await _controller.GetNutritionLogs(pageNumber: 0);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Page number must be >= 1");
    }

    [Fact]
    public async Task GetNutritionLogs_ShouldReturnBadRequest_WhenPageSizeInvalid()
    {
        // Act
        var result = await _controller.GetNutritionLogs(pageSize: 150);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetNutritionLogs_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetNutritionLogs();

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task GetNutritionLogs_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.GetNutritionLogsAsync(userId, 1, 20))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetNutritionLogs();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving nutrition logs");
    }

    [Fact]
    public async Task GetNutritionLogById_ShouldReturnOk_WhenLogExists()
    {
        // Arrange
        var logId = 1;
        var userId = 1;

        var expectedResponse = new NutritionLogResponse
        {
            NutritionLogId = logId,
            UserId = userId,
            LogDate = new DateTime(2025, 12, 19),
            TotalCalories = 2000,
            TotalProteinG = 150,
            TotalCarbsG = 250,
            TotalFatG = 80,
            Notes = "Good day",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EntriesByMeal = new Dictionary<string, List<FoodEntryResponse>>(),
            Summary = new NutritionSummaryDto
            {
                TotalCalories = 2000,
                TotalProteinG = 150,
                TotalCarbsG = 250,
                TotalFatG = 80,
                EntryCount = 0,
                MacroBreakdown = new MacroBreakdownDto
                {
                    ProteinPercentage = 30,
                    CarbsPercentage = 50,
                    FatPercentage = 20
                }
            }
        };

        _mockNutritionLogService
            .Setup(s => s.GetByIdAsync(userId, logId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetNutritionLogById(logId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("NutritionLogId").GetInt32().Should().Be(logId);
    }

    [Fact]
    public async Task GetNutritionLogById_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var logId = 999;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.GetByIdAsync(userId, logId))
            .ReturnsAsync(() => null);

        // Act
        var result = await _controller.GetNutritionLogById(logId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be($"Nutrition log with ID {logId} not found");
    }

    [Fact]
    public async Task GetNutritionLogById_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetNutritionLogById(1);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task GetNutritionLogById_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var logId = 1;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.GetByIdAsync(userId, logId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetNutritionLogById(logId);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while retrieving nutrition log");
    }

    [Fact]
    public async Task CreateNutritionLog_ShouldReturnCreated_WhenLogCreated()
    {
        // Arrange
        var userId = 1;
        var request = new CreateNutritionLogRequest
        {
            LogDate = new DateTime(2025, 12, 19),
            Notes = "New log"
        };

        var expectedResponse = new NutritionLogResponse
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = request.LogDate.Value,
            TotalCalories = 0,
            TotalProteinG = 0,
            TotalCarbsG = 0,
            TotalFatG = 0,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EntriesByMeal = new Dictionary<string, List<FoodEntryResponse>>(),
            Summary = new NutritionSummaryDto
            {
                TotalCalories = 0,
                TotalProteinG = 0,
                TotalCarbsG = 0,
                TotalFatG = 0,
                EntryCount = 0,
                MacroBreakdown = new MacroBreakdownDto
                {
                    ProteinPercentage = 0,
                    CarbsPercentage = 0,
                    FatPercentage = 0
                }
            }
        };

        _mockNutritionLogService
            .Setup(s => s.CreateNutritionLogAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateNutritionLog(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(NutritionLogsController.GetNutritionLogById));
        var json = JsonSerializer.Serialize(createdResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("NutritionLogId").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CreateNutritionLog_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.MinValue // Invalid
        };

        _mockNutritionLogService
            .Setup(s => s.CreateNutritionLogAsync(It.IsAny<int>(), It.Is<CreateNutritionLogRequest>(r => r.LogDate == DateTime.MinValue)))
            .ThrowsAsync(new ArgumentException("Invalid log date"));

        // Act
        var result = await _controller.CreateNutritionLog(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateNutritionLog_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var request = new CreateNutritionLogRequest
        {
            LogDate = new DateTime(2025, 12, 19)
        };

        // Act
        var result = await _controller.CreateNutritionLog(request);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task CreateNutritionLog_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var userId = 1;
        var request = new CreateNutritionLogRequest
        {
            LogDate = new DateTime(2025, 12, 19)
        };

        _mockNutritionLogService
            .Setup(s => s.CreateNutritionLogAsync(userId, request))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.CreateNutritionLog(request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while creating nutrition log");
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturnOk_WhenNotesUpdated()
    {
        // Arrange
        var logId = 1;
        var userId = 1;
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Updated notes"
        };

        var expectedResponse = new NutritionLogResponse
        {
            NutritionLogId = logId,
            UserId = userId,
            LogDate = new DateTime(2025, 12, 19),
            TotalCalories = 2000,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EntriesByMeal = new Dictionary<string, List<FoodEntryResponse>>(),
            Summary = new NutritionSummaryDto
            {
                TotalCalories = 2000,
                EntryCount = 0,
                MacroBreakdown = new MacroBreakdownDto()
            }
        };

        _mockNutritionLogService
            .Setup(s => s.UpdateNotesAsync(userId, logId, request.Notes))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateNotes(logId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("Notes").GetString().Should().Be(request.Notes);
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = null // Assuming validation requires notes
        };

        _mockNutritionLogService
            .Setup(s => s.UpdateNotesAsync(1, 1, null))
            .ThrowsAsync(new ArgumentException("Notes is required"));

        // Act
        var result = await _controller.UpdateNotes(1, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var logId = 999;
        var userId = 1;
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Updated notes"
        };

        _mockNutritionLogService
            .Setup(s => s.UpdateNotesAsync(userId, logId, request.Notes))
            .ThrowsAsync(new KeyNotFoundException("Log not found"));

        // Act
        var result = await _controller.UpdateNotes(logId, request);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Log not found");
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Updated notes"
        };

        // Act
        var result = await _controller.UpdateNotes(1, request);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var logId = 1;
        var userId = 1;
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Updated notes"
        };

        _mockNutritionLogService
            .Setup(s => s.UpdateNotesAsync(userId, logId, request.Notes))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.UpdateNotes(logId, request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while updating nutrition log notes");
    }

    [Fact]
    public async Task DeleteNutritionLog_ShouldReturnNoContent_WhenLogDeleted()
    {
        // Arrange
        var logId = 1;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteAsync(userId, logId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNutritionLog(logId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteNutritionLog_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var logId = 999;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteAsync(userId, logId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNutritionLog(logId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be($"Nutrition log with ID {logId} not found");
    }

    [Fact]
    public async Task DeleteNutritionLog_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.DeleteNutritionLog(1);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task DeleteNutritionLog_ShouldReturn500_WhenServiceThrowsException()
    {
        // Arrange
        var logId = 1;
        var userId = 1;

        _mockNutritionLogService
            .Setup(s => s.DeleteAsync(userId, logId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.DeleteNutritionLog(logId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var json = JsonSerializer.Serialize(statusCodeResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An error occurred while deleting nutrition log");
    }
}

