using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HealthSync.Application.Tests.Services;

public class NutritionLogServiceTests
{
    private readonly Mock<INutritionLogRepository> _nutritionLogRepositoryMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<ILogger<NutritionLogService>> _loggerMock;
    private readonly NutritionLogService _service;

    public NutritionLogServiceTests()
    {
        _nutritionLogRepositoryMock = new Mock<INutritionLogRepository>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _loggerMock = new Mock<ILogger<NutritionLogService>>();

        _service = new NutritionLogService(
            _nutritionLogRepositoryMock.Object,
            _foodItemRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AddFoodEntryAsync_ShouldCalculateMacrosCorrectly_WhenQuantityIs1_5()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Lunch",
            Quantity = 1.5m,
            Notes = "Test entry"
        };

        var foodItem = new FoodItem
        {
            FoodItemId = 1,
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        var nutritionLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync(nutritionLog);

        _foodItemRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(1))
            .ReturnsAsync(foodItem);

        _nutritionLogRepositoryMock
            .Setup(r => r.AddFoodEntryAsync(It.IsAny<FoodEntry>()))
            .Returns(Task.CompletedTask);

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(nutritionLog);

        _nutritionLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<NutritionLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddFoodEntryAsync(userId, date, request);

        // Assert
        result.Should().NotBeNull();
        result.Calories.Should().Be(247.5m); // 1.5 * 165
        result.ProteinG.Should().Be(46.5m); // 1.5 * 31
        result.CarbsG.Should().Be(0); // 1.5 * 0
        result.FatG.Should().Be(5.4m); // 1.5 * 3.6
        result.Quantity.Should().Be(1.5m);
        result.MealType.Should().Be("Lunch");
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldSubtractTotalsCorrectly_WhenRemovingEntry()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;

        var nutritionLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = new DateTime(2025, 11, 2),
            TotalCalories = 295,
            TotalProteinG = 33.7m,
            TotalCarbsG = 28,
            TotalFatG = 3.9m,
            FoodEntries = new List<FoodEntry>
            {
                new FoodEntry
                {
                    FoodEntryId = 1,
                    NutritionLogId = 1,
                    FoodItemId = 1,
                    Quantity = 1,
                    Calories = 165,
                    ProteinG = 31,
                    CarbsG = 0,
                    FatG = 3.6m
                },
                new FoodEntry
                {
                    FoodEntryId = 2,
                    NutritionLogId = 1,
                    FoodItemId = 2,
                    Quantity = 1,
                    Calories = 130,
                    ProteinG = 2.7m,
                    CarbsG = 28,
                    FatG = 0.3m
                }
            }
        };

        // Set navigation properties
        foreach (var entry in nutritionLog.FoodEntries)
        {
            entry.NutritionLog = nutritionLog;
        }

        _nutritionLogRepositoryMock
            .Setup(r => r.GetFoodEntryByIdAsync(entryId))
            .ReturnsAsync(nutritionLog.FoodEntries.First(e => e.FoodEntryId == entryId));

        _nutritionLogRepositoryMock
            .Setup(r => r.DeleteFoodEntryAsync(entryId))
            .Returns(Task.FromResult(true));

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(() =>
            {
                // Simulate removing entry
                var entryToRemove = nutritionLog.FoodEntries.FirstOrDefault(e => e.FoodEntryId == entryId);
                if (entryToRemove != null)
                {
                    nutritionLog.FoodEntries.Remove(entryToRemove);
                }
                return nutritionLog;
            });

        _nutritionLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<NutritionLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteFoodEntryAsync(userId, entryId);

        // Assert
        result.Should().BeTrue();
        _nutritionLogRepositoryMock.Verify(r => r.UpdateAsync(It.Is<NutritionLog>(log =>
            log.TotalCalories == 130 && // 295 - 165
            log.TotalProteinG == 2.7m && // 33.7 - 31
            log.TotalCarbsG == 28 && // 28 - 0
            log.TotalFatG == 0.3m // 3.9 - 3.6
        )), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDailyLogAsync_ShouldReturnExistingLog_WhenLogExists()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);

        var existingLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = date,
            TotalCalories = 500,
            TotalProteinG = 50,
            TotalCarbsG = 50,
            TotalFatG = 20,
            Notes = "Existing log",
            CreatedAt = DateTime.UtcNow,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync(existingLog);

        // Act
        var result = await _service.GetOrCreateDailyLogAsync(userId, date);

        // Assert
        result.Should().NotBeNull();
        result.NutritionLogId.Should().Be(1);
        result.TotalCalories.Should().Be(500);
        result.TotalProteinG.Should().Be(50);
        result.TotalCarbsG.Should().Be(50);
        result.TotalFatG.Should().Be(20);
        result.Notes.Should().Be("Existing log");
    }

    [Fact]
    public async Task GetOrCreateDailyLogAsync_ShouldCreateNewLog_WhenLogDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync((NutritionLog?)null);

        var newLog = new NutritionLog
        {
            NutritionLogId = 2,
            UserId = userId,
            LogDate = date,
            TotalCalories = 0,
            TotalProteinG = 0,
            TotalCarbsG = 0,
            TotalFatG = 0,
            Notes = null,
            CreatedAt = DateTime.UtcNow,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<NutritionLog>()))
            .ReturnsAsync(newLog);

        // Act
        var result = await _service.GetOrCreateDailyLogAsync(userId, date);

        // Assert
        result.Should().NotBeNull();
        result.NutritionLogId.Should().Be(2);
        result.TotalCalories.Should().Be(0);
        result.TotalProteinG.Should().Be(0);
        result.TotalCarbsG.Should().Be(0);
        result.TotalFatG.Should().Be(0);
        result.Notes.Should().BeNull();
    }
}

