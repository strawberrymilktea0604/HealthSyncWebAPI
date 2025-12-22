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

    [Fact]
    public async Task CreateNutritionLogAsync_ShouldThrowException_WhenLogAlreadyExists()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);
        var request = new CreateNutritionLogRequest
        {
            LogDate = date,
            Notes = "Test log",
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        var existingLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync(existingLog);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateNutritionLogAsync(userId, request));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateNutritionLogAsync_ShouldUseTodayDate_WhenLogDateIsNull()
    {
        // Arrange
        var userId = 1;
        var request = new CreateNutritionLogRequest
        {
            LogDate = null, // Null date
            Notes = "Test log",
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, It.IsAny<DateTime>()))
            .ReturnsAsync((NutritionLog?)null);

        _nutritionLogRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<NutritionLog>()))
            .ReturnsAsync((NutritionLog log) =>
            {
                log.NutritionLogId = 1;
                return log;
            });

        // Act
        var result = await _service.CreateNutritionLogAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.LogDate.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task AddFoodEntryAsync_ShouldThrowException_WhenFoodItemNotFound()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 999, // Non-existent
            MealType = "Lunch",
            Quantity = 1
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
            .Setup(r => r.GetEntityByIdAsync(999))
            .ReturnsAsync((FoodItem?)null); // Food item not found

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddFoodEntryAsync(userId, date, request));

        exception.Message.Should().Contain("Food item 999 not found");
    }

    [Fact]
    public async Task AddFoodEntryAsync_ShouldCreateNutritionLog_WhenLogDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 2);
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = 2
        };

        var foodItem = new FoodItem
        {
            FoodItemId = 1,
            Name = "Egg",
            Category = "Protein",
            ServingSize = 1,
            ServingUnit = ServingUnit.Piece,
            CaloriesPerServing = 70,
            ProteinG = 6,
            CarbsG = 0.6m,
            FatG = 5
        };

        var newLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync((NutritionLog?)null); // Log doesn't exist

        _nutritionLogRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<NutritionLog>()))
            .ReturnsAsync(newLog);

        _foodItemRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(1))
            .ReturnsAsync(foodItem);

        _nutritionLogRepositoryMock
            .Setup(r => r.AddFoodEntryAsync(It.IsAny<FoodEntry>()))
            .Returns(Task.CompletedTask);

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(newLog);

        _nutritionLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<NutritionLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddFoodEntryAsync(userId, date, request);

        // Assert
        result.Should().NotBeNull();
        result.Calories.Should().Be(140); // 2 * 70
        _nutritionLogRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<NutritionLog>()), Times.Once);
    }

    [Fact]
    public async Task GetNutritionLogsAsync_ShouldReturnPaginatedResult()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 1;
        var pageSize = 10;

        var logs = new List<NutritionLog>
        {
            new NutritionLog
            {
                NutritionLogId = 1,
                UserId = userId,
                LogDate = new DateTime(2025, 11, 1),
                FoodEntries = new List<FoodEntry>()
            },
            new NutritionLog
            {
                NutritionLogId = 2,
                UserId = userId,
                LogDate = new DateTime(2025, 11, 2),
                FoodEntries = new List<FoodEntry>()
            }
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, pageNumber, pageSize))
            .ReturnsAsync((logs, 15)); // 15 total items

        // Act
        var result = await _service.GetNutritionLogsAsync(userId, pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenLogNotFound()
    {
        // Arrange
        var userId = 1;
        var logId = 999;

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync((NutritionLog?)null);

        // Act
        var result = await _service.GetByIdAsync(userId, logId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var logId = 1;

        var log = new NutritionLog
        {
            NutritionLogId = logId,
            UserId = 999, // Different user
            LogDate = DateTime.UtcNow.Date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync(log);

        // Act
        var result = await _service.GetByIdAsync(userId, logId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateNotesAsync_ShouldThrowException_WhenLogNotFound()
    {
        // Arrange
        var userId = 1;
        var logId = 999;

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync((NutritionLog?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateNotesAsync(userId, logId, "New notes"));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenLogNotFound()
    {
        // Arrange
        var userId = 1;
        var logId = 999;

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync((NutritionLog?)null);

        // Act
        var result = await _service.DeleteAsync(userId, logId);

        // Assert
        result.Should().BeFalse();
        _nutritionLogRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var logId = 1;

        var log = new NutritionLog
        {
            NutritionLogId = logId,
            UserId = 999, // Different user
            LogDate = DateTime.UtcNow.Date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync(log);

        // Act
        var result = await _service.DeleteAsync(userId, logId);

        // Assert
        result.Should().BeFalse();
        _nutritionLogRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldReturnFalse_WhenEntryNotFound()
    {
        // Arrange
        var userId = 1;
        var entryId = 999;

        _nutritionLogRepositoryMock
            .Setup(r => r.GetFoodEntryByIdAsync(entryId))
            .ReturnsAsync((FoodEntry?)null);

        // Act
        var result = await _service.DeleteFoodEntryAsync(userId, entryId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldReturnFalse_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;

        var nutritionLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = 999, // Different user
            LogDate = DateTime.UtcNow.Date,
            FoodEntries = new List<FoodEntry>()
        };

        var entry = new FoodEntry
        {
            FoodEntryId = entryId,
            NutritionLogId = 1,
            NutritionLog = nutritionLog
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetFoodEntryByIdAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.DeleteFoodEntryAsync(userId, entryId);

        // Assert
        result.Should().BeFalse();
    }
}

