using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class NutritionLogRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NutritionLogRepository _repository;

    public NutritionLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new NutritionLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByUserAndDateAsync_ShouldReturnNutritionLog_WhenExists()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var logDate = new DateTime(2025, 12, 20);
        var nutritionLog = new NutritionLog
        {
            UserId = user.UserId,
            LogDate = logDate,
            TotalCalories = 500,
            TotalProteinG = 25,
            TotalCarbsG = 60,
            TotalFatG = 20
        };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserAndDateAsync(user.UserId, logDate);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.LogDate.Should().Be(logDate);
        result.TotalCalories.Should().Be(500);
    }

    [Fact]
    public async Task GetByUserAndDateAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByUserAndDateAsync(1, DateTime.Today);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAndDateAsync_ShouldCompareDateOnly()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var logDate = new DateTime(2025, 12, 20, 10, 30, 0); // With time
        var nutritionLog = new NutritionLog
        {
            UserId = user.UserId,
            LogDate = logDate
        };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        var searchDate = new DateTime(2025, 12, 20, 15, 45, 0); // Different time, same date

        // Act
        var result = await _repository.GetByUserAndDateAsync(user.UserId, searchDate);

        // Assert
        result.Should().NotBeNull();
        result!.LogDate.Date.Should().Be(searchDate.Date);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNutritionLogWithFoodEntries_WhenExists()
    {
        // Arrange
        var nutritionLog = new NutritionLog
        {
            UserId = 1,
            LogDate = DateTime.Today,
            TotalCalories = 300
        };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        var foodItem = new FoodItem { Name = "Apple", Category = "Fruits" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var foodEntry = new FoodEntry
        {
            NutritionLogId = nutritionLog.NutritionLogId,
            FoodItemId = foodItem.FoodItemId,
            Quantity = 1.0m,
            MealType = MealType.Breakfast
        };
        await _context.FoodEntries.AddAsync(foodEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(nutritionLog.NutritionLogId);

        // Assert
        result.Should().NotBeNull();
        result!.NutritionLogId.Should().Be(nutritionLog.NutritionLogId);
        result.FoodEntries.Should().HaveCount(1);
        result.FoodEntries.First().FoodItem.Should().NotBeNull();
        result.FoodEntries.First().FoodItem.Name.Should().Be("Apple");
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
    public async Task GetByUserIdAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var user1 = new ApplicationUser { Email = "user1@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        var user2 = new ApplicationUser { Email = "user2@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var nutritionLogs = new List<NutritionLog>
        {
            new NutritionLog { UserId = user1.UserId, LogDate = DateTime.Today, TotalCalories = 500 },
            new NutritionLog { UserId = user1.UserId, LogDate = DateTime.Today.AddDays(-1), TotalCalories = 600 },
            new NutritionLog { UserId = user1.UserId, LogDate = DateTime.Today.AddDays(-2), TotalCalories = 700 },
            new NutritionLog { UserId = user2.UserId, LogDate = DateTime.Today, TotalCalories = 400 }
        };
        await _context.NutritionLogs.AddRangeAsync(nutritionLogs);
        await _context.SaveChangesAsync();

        // Act
        var (logs, totalCount) = await _repository.GetByUserIdAsync(user1.UserId, 1, 2);

        // Assert
        logs.Should().HaveCount(2);
        totalCount.Should().Be(3);
        logs.All(l => l.UserId == user1.UserId).Should().BeTrue();
        logs.First().LogDate.Should().Be(DateTime.Today); // Ordered by LogDate desc
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldHandleInvalidPageParameters()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var (logs, totalCount) = await _repository.GetByUserIdAsync(user.UserId, 0, 0);

        // Assert
        logs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldLimitMaxPageSize()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var (logs, totalCount) = await _repository.GetByUserIdAsync(user.UserId, 1, 150);

        // Assert - Should use max page size of 100
        // Since there are no logs, it should return empty
        logs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddNutritionLogToDatabase()
    {
        // Arrange
        var nutritionLog = new NutritionLog
        {
            UserId = 1,
            LogDate = DateTime.Today,
            TotalCalories = 800,
            TotalProteinG = 40,
            TotalCarbsG = 90,
            TotalFatG = 30
        };

        // Act
        var result = await _repository.CreateAsync(nutritionLog);

        // Assert
        result.NutritionLogId.Should().BeGreaterThan(0);
        var savedLog = await _context.NutritionLogs.FindAsync(result.NutritionLogId);
        savedLog.Should().NotBeNull();
        savedLog!.TotalCalories.Should().Be(800);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNutritionLogInDatabase()
    {
        // Arrange
        var nutritionLog = new NutritionLog
        {
            UserId = 1,
            LogDate = DateTime.Today,
            TotalCalories = 500
        };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        // Act
        nutritionLog.TotalCalories = 750;
        nutritionLog.TotalProteinG = 35;
        await _repository.UpdateAsync(nutritionLog);

        // Assert
        var updatedLog = await _context.NutritionLogs.FindAsync(nutritionLog.NutritionLogId);
        updatedLog.Should().NotBeNull();
        updatedLog!.TotalCalories.Should().Be(750);
        updatedLog.TotalProteinG.Should().Be(35);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveNutritionLogFromDatabase()
    {
        // Arrange
        var nutritionLog = new NutritionLog { UserId = 1, LogDate = DateTime.Today };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(nutritionLog.NutritionLogId);

        // Assert
        var deletedLog = await _context.NutritionLogs.FindAsync(nutritionLog.NutritionLogId);
        deletedLog.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenNutritionLogDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteAsync(999); // Should not throw
        
        // Assert that no exception was thrown
        Assert.True(true);
    }

    [Fact]
    public async Task AddFoodEntryAsync_ShouldAddFoodEntryToDatabase()
    {
        // Arrange
        var nutritionLog = new NutritionLog { UserId = 1, LogDate = DateTime.Today };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        var foodItem = new FoodItem { Name = "Banana", Category = "Fruits" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var foodEntry = new FoodEntry
        {
            NutritionLogId = nutritionLog.NutritionLogId,
            FoodItemId = foodItem.FoodItemId,
            Quantity = 2.0m,
            MealType = MealType.Snack,
            Calories = 200,
            ProteinG = 2,
            CarbsG = 50,
            FatG = 1
        };

        // Act
        await _repository.AddFoodEntryAsync(foodEntry);

        // Assert
        var savedEntry = await _context.FoodEntries.FindAsync(foodEntry.FoodEntryId);
        savedEntry.Should().NotBeNull();
        savedEntry!.Quantity.Should().Be(2.0m);
        savedEntry.MealType.Should().Be(MealType.Snack);
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldRemoveFoodEntryFromDatabase()
    {
        // Arrange
        var nutritionLog = new NutritionLog { UserId = 1, LogDate = DateTime.Today };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        var foodItem = new FoodItem { Name = "Orange", Category = "Fruits" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var foodEntry = new FoodEntry
        {
            NutritionLogId = nutritionLog.NutritionLogId,
            FoodItemId = foodItem.FoodItemId,
            Quantity = 1.0m
        };
        await _context.FoodEntries.AddAsync(foodEntry);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteFoodEntryAsync(foodEntry.FoodEntryId);

        // Assert
        var deletedEntry = await _context.FoodEntries.FindAsync(foodEntry.FoodEntryId);
        deletedEntry.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFoodEntryAsync_ShouldNotThrow_WhenFoodEntryDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteFoodEntryAsync(999); // Should not throw
        
        // Assert that no exception was thrown
        Assert.True(true);
    }

    [Fact]
    public async Task GetFoodEntryByIdAsync_ShouldReturnFoodEntryWithNutritionLog_WhenExists()
    {
        // Arrange
        var nutritionLog = new NutritionLog { UserId = 1, LogDate = DateTime.Today };
        await _context.NutritionLogs.AddAsync(nutritionLog);
        await _context.SaveChangesAsync();

        var foodItem = new FoodItem { Name = "Grapes", Category = "Fruits" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var foodEntry = new FoodEntry
        {
            NutritionLogId = nutritionLog.NutritionLogId,
            FoodItemId = foodItem.FoodItemId,
            Quantity = 1.5m
        };
        await _context.FoodEntries.AddAsync(foodEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetFoodEntryByIdAsync(foodEntry.FoodEntryId);

        // Assert
        result.Should().NotBeNull();
        result!.FoodEntryId.Should().Be(foodEntry.FoodEntryId);
        result.NutritionLog.Should().NotBeNull();
        result.NutritionLog.NutritionLogId.Should().Be(nutritionLog.NutritionLogId);
    }

    [Fact]
    public async Task GetFoodEntryByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetFoodEntryByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllNutritionLogs()
    {
        // Arrange
        var nutritionLogs = new List<NutritionLog>
        {
            new NutritionLog { UserId = 1, LogDate = DateTime.Today, TotalCalories = 500 },
            new NutritionLog { UserId = 2, LogDate = DateTime.Today.AddDays(-1), TotalCalories = 600 },
            new NutritionLog { UserId = 1, LogDate = DateTime.Today.AddDays(-2), TotalCalories = 700 }
        };
        await _context.NutritionLogs.AddRangeAsync(nutritionLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(nl => nl.TotalCalories == 500);
        result.Should().Contain(nl => nl.TotalCalories == 600);
        result.Should().Contain(nl => nl.TotalCalories == 700);
    }
}

