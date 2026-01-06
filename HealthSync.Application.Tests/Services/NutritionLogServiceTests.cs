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

    #region Additional Tests for 100% Coverage

    [Fact]
    public async Task CreateNutritionLogAsync_ShouldCreateLogWithFoodEntries_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var date = new DateTime(2025, 11, 3);
        var request = new CreateNutritionLogRequest
        {
            LogDate = date,
            Notes = "Test log with entries",
            FoodEntries = new List<CreateFoodEntryRequest>
            {
                new CreateFoodEntryRequest
                {
                    FoodItemId = 1,
                    MealType = "Breakfast",
                    Quantity = 2,
                    ConsumedAt = DateTime.UtcNow,
                    Notes = "Eggs"
                },
                new CreateFoodEntryRequest
                {
                    FoodItemId = 2,
                    MealType = "Lunch",
                    Quantity = 1,
                    ConsumedAt = DateTime.UtcNow.AddHours(4),
                    Notes = "Chicken"
                }
            }
        };

        var foodItem1 = new FoodItem
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

        var foodItem2 = new FoodItem
        {
            FoodItemId = 2,
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserAndDateAsync(userId, date))
            .ReturnsAsync((NutritionLog?)null);

        _nutritionLogRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<NutritionLog>()))
            .ReturnsAsync((NutritionLog log) =>
            {
                log.NutritionLogId = 1;
                // Populate food entries with FoodItem data
                foreach (var entry in log.FoodEntries)
                {
                    if (entry.FoodItemId == 1)
                        entry.FoodItem = foodItem1;
                    else if (entry.FoodItemId == 2)
                        entry.FoodItem = foodItem2;
                }
                return log;
            });

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((NutritionLog log) =>
            {
                var newLog = new NutritionLog
                {
                    NutritionLogId = 1,
                    UserId = userId,
                    LogDate = date,
                    Notes = "Test log with entries",
                    FoodEntries = new List<FoodEntry>
                    {
                        new FoodEntry
                        {
                            FoodEntryId = 1,
                            FoodItemId = 1,
                            MealType = MealType.Breakfast,
                            Quantity = 2,
                            Calories = 140,
                            ProteinG = 12,
                            CarbsG = 1.2m,
                            FatG = 10,
                            FoodItem = foodItem1
                        },
                        new FoodEntry
                        {
                            FoodEntryId = 2,
                            FoodItemId = 2,
                            MealType = MealType.Lunch,
                            Quantity = 1,
                            Calories = 165,
                            ProteinG = 31,
                            CarbsG = 0,
                            FatG = 3.6m,
                            FoodItem = foodItem2
                        }
                    }
                };
                return newLog;
            });

        _nutritionLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<NutritionLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateNutritionLogAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.NutritionLogId.Should().Be(1);
        result.Notes.Should().Be("Test log with entries");
        result.EntriesByMeal.Should().ContainKey("Breakfast");
        result.EntriesByMeal.Should().ContainKey("Lunch");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnLog_WhenLogExists()
    {
        // Arrange
        var userId = 1;
        var logId = 1;

        var foodItem = new FoodItem
        {
            FoodItemId = 1,
            Name = "Test Food",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = 100,
            ProteinG = 20,
            CarbsG = 5,
            FatG = 2
        };

        var log = new NutritionLog
        {
            NutritionLogId = logId,
            UserId = userId,
            LogDate = DateTime.UtcNow.Date,
            TotalCalories = 500,
            TotalProteinG = 50,
            TotalCarbsG = 30,
            TotalFatG = 20,
            Notes = "Test log",
            CreatedAt = DateTime.UtcNow,
            FoodEntries = new List<FoodEntry>
            {
                new FoodEntry
                {
                    FoodEntryId = 1,
                    NutritionLogId = logId,
                    FoodItemId = 1,
                    FoodItem = foodItem,
                    MealType = MealType.Breakfast,
                    Quantity = 2,
                    Calories = 200,
                    ProteinG = 40,
                    CarbsG = 10,
                    FatG = 4,
                    ConsumedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync(log);

        // Act
        var result = await _service.GetByIdAsync(userId, logId);

        // Assert
        result.Should().NotBeNull();
        result!.NutritionLogId.Should().Be(logId);
        result.TotalCalories.Should().Be(500);
        result.TotalProteinG.Should().Be(50);
        result.TotalCarbsG.Should().Be(30);
        result.TotalFatG.Should().Be(20);
        result.Notes.Should().Be("Test log");
        result.EntriesByMeal.Should().ContainKey("Breakfast");
    }

    [Fact]
    public async Task UpdateNotesAsync_ShouldUpdateNotes_WhenLogExists()
    {
        // Arrange
        var userId = 1;
        var logId = 1;
        var newNotes = "Updated notes";

        var log = new NutritionLog
        {
            NutritionLogId = logId,
            UserId = userId,
            LogDate = DateTime.UtcNow.Date,
            Notes = "Old notes",
            CreatedAt = DateTime.UtcNow,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync(log);

        _nutritionLogRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<NutritionLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateNotesAsync(userId, logId, newNotes);

        // Assert
        result.Should().NotBeNull();
        result.Notes.Should().Be(newNotes);
        _nutritionLogRepositoryMock.Verify(r => r.UpdateAsync(It.Is<NutritionLog>(l =>
            l.Notes == newNotes)), Times.Once);
    }

    [Fact]
    public async Task UpdateNotesAsync_ShouldThrowException_WhenUserIdMismatch()
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateNotesAsync(userId, logId, "New notes"));
        
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteLog_WhenLogExists()
    {
        // Arrange
        var userId = 1;
        var logId = 1;

        var log = new NutritionLog
        {
            NutritionLogId = logId,
            UserId = userId,
            LogDate = DateTime.UtcNow.Date,
            FoodEntries = new List<FoodEntry>()
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(logId))
            .ReturnsAsync(log);

        _nutritionLogRepositoryMock
            .Setup(r => r.DeleteAsync(logId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(userId, logId);

        // Assert
        result.Should().BeTrue();
        _nutritionLogRepositoryMock.Verify(r => r.DeleteAsync(logId), Times.Once);
    }

    [Fact]
    public async Task GetNutritionLogsAsync_ShouldCalculatePaginationCorrectly()
    {
        // Arrange
        var userId = 1;
        var pageNumber = 2;
        var pageSize = 5;

        var logs = new List<NutritionLog>
        {
            new NutritionLog
            {
                NutritionLogId = 6,
                UserId = userId,
                LogDate = new DateTime(2025, 11, 6),
                FoodEntries = new List<FoodEntry>()
            }
        };

        _nutritionLogRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, pageNumber, pageSize))
            .ReturnsAsync((logs, 8)); // 8 total items

        // Act
        var result = await _service.GetNutritionLogsAsync(userId, pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalItems.Should().Be(8);
        result.TotalPages.Should().Be(2);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldRecalculateTotals_WhenLogIsNull()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;

        var nutritionLog = new NutritionLog
        {
            NutritionLogId = 1,
            UserId = userId,
            LogDate = DateTime.UtcNow.Date,
            TotalCalories = 165,
            TotalProteinG = 31,
            TotalCarbsG = 0,
            TotalFatG = 3.6m,
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
            .ReturnsAsync(nutritionLog.FoodEntries.First());

        _nutritionLogRepositoryMock
            .Setup(r => r.DeleteFoodEntryAsync(entryId))
            .Returns(Task.FromResult(true));

        // After delete, log returns null
        _nutritionLogRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((NutritionLog?)null);

        // Act
        var result = await _service.DeleteFoodEntryAsync(userId, entryId);

        // Assert
        result.Should().BeTrue();
        // UpdateAsync should not be called since log is null
        _nutritionLogRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<NutritionLog>()), Times.Never);
    }

    #endregion
}

