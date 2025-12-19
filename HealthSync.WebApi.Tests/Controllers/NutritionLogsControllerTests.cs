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
}