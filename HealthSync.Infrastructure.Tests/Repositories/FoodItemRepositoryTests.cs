using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class FoodItemRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FoodItemRepository _repository;

    public FoodItemRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new FoodItemRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnAllItems_WhenNoFilters()
    {
        // Arrange
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Name = "Chicken Breast", Category = "Protein", CaloriesPerServing = 165 },
            new FoodItem { Name = "Brown Rice", Category = "Carbs", CaloriesPerServing = 216 },
            new FoodItem { Name = "Broccoli", Category = "Vegetables", CaloriesPerServing = 55 }
        };
        await _context.FoodItems.AddRangeAsync(foodItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(3);
        result.Items.All(item => item is FoodItemDto).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Name = "Chicken Breast", Category = "Protein", Description = "Lean protein source" },
            new FoodItem { Name = "Chicken Thigh", Category = "Protein", Description = "Dark meat" },
            new FoodItem { Name = "Brown Rice", Category = "Carbs", Description = "Whole grain" }
        };
        await _context.FoodItems.AddRangeAsync(foodItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("chicken", null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.Items.All(item => item.Name.Contains("Chicken", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByCategory()
    {
        // Arrange
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Name = "Chicken Breast", Category = "Protein" },
            new FoodItem { Name = "Salmon", Category = "Protein" },
            new FoodItem { Name = "Brown Rice", Category = "Carbs" }
        };
        await _context.FoodItems.AddRangeAsync(foodItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, "Protein", 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.Items.All(item => item.Category == "Protein").Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldSupportPagination()
    {
        // Arrange
        var foodItems = new List<FoodItem>();
        for (int i = 1; i <= 10; i++)
        {
            foodItems.Add(new FoodItem { Name = $"Food {i:D2}", Category = "Protein" });
        }
        await _context.FoodItems.AddRangeAsync(foodItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, 2, 3);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(10);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.Items.First().Name.Should().Be("Food 04");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFoodItemDto_WhenExists()
    {
        // Arrange
        var foodItem = new FoodItem
        {
            Name = "Chicken Breast",
            Category = "Protein",
            Description = "Lean protein",
            ServingSize = 100,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(foodItem.FoodItemId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Chicken Breast");
        result.Category.Should().Be("Protein");
        result.CaloriesPerServing.Should().Be(165);
        result.ProteinG.Should().Be(31);
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
    public async Task GetEntityByIdAsync_ShouldReturnFoodItemEntity_WhenExists()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Chicken Breast", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEntityByIdAsync(foodItem.FoodItemId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Chicken Breast");
        result.Should().BeOfType<FoodItem>();
    }

    [Fact]
    public async Task GetEntityByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetEntityByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddFoodItemToDatabase()
    {
        // Arrange
        var foodItem = new FoodItem
        {
            Name = "Salmon",
            Category = "Protein",
            ServingSize = 150,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = 280
        };

        // Act
        var result = await _repository.CreateAsync(foodItem);

        // Assert
        result.FoodItemId.Should().BeGreaterThan(0);
        var savedItem = await _context.FoodItems.FindAsync(result.FoodItemId);
        savedItem.Should().NotBeNull();
        savedItem!.Name.Should().Be("Salmon");
    }

    [Fact]
    public async Task AddAsync_ShouldAddFoodItemToDatabase()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Brown Rice", Category = "Carbs" };

        // Act
        var result = await _repository.AddAsync(foodItem);

        // Assert
        result.FoodItemId.Should().BeGreaterThan(0);
        var savedItem = await _context.FoodItems.FindAsync(result.FoodItemId);
        savedItem.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithId_ShouldUpdateExistingFoodItem()
    {
        // Arrange
        var foodItem = new FoodItem
        {
            Name = "Old Name",
            Category = "Protein",
            CaloriesPerServing = 100
        };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var updatedItem = new FoodItem
        {
            Name = "New Name",
            Category = "Protein",
            CaloriesPerServing = 200
        };

        // Act
        var result = await _repository.UpdateAsync(foodItem.FoodItemId, updatedItem);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.CaloriesPerServing.Should().Be(200);
    }

    [Fact]
    public async Task UpdateAsync_WithId_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Test", Category = "Protein" };

        // Act
        var result = await _repository.UpdateAsync(999, foodItem);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithEntity_ShouldUpdateFoodItem()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Old Name", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        foodItem.Name = "Updated Name";
        await _repository.UpdateAsync(foodItem);

        // Assert
        var updatedItem = await _context.FoodItems.FindAsync(foodItem.FoodItemId);
        updatedItem.Should().NotBeNull();
        updatedItem!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFoodItemFromDatabase()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Test Food", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(foodItem.FoodItemId);

        // Assert
        result.Should().BeTrue();
        var deletedItem = await _context.FoodItems.FindAsync(foodItem.FoodItemId);
        deletedItem.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenNameExists()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Chicken Breast", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync("Chicken Breast");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnFalse_WhenNameDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("Non-existent Food");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ShouldReturnFalse_WhenOnlyExcludedItemHasName()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Chicken Breast", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync("Chicken Breast", foodItem.FoodItemId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUsedInFoodEntriesAsync_ShouldReturnTrue_WhenFoodItemIsUsed()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Chicken Breast", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        var nutritionLog = new NutritionLog { UserId = 1, LogDate = DateTime.Today };
        await _context.NutritionLogs.AddAsync(nutritionLog);
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
        var result = await _repository.IsUsedInFoodEntriesAsync(foodItem.FoodItemId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUsedInFoodEntriesAsync_ShouldReturnFalse_WhenFoodItemIsNotUsed()
    {
        // Arrange
        var foodItem = new FoodItem { Name = "Chicken Breast", Category = "Protein" };
        await _context.FoodItems.AddAsync(foodItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUsedInFoodEntriesAsync(foodItem.FoodItemId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldReturnFoodItemsByIds()
    {
        // Arrange
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Name = "Food 1", Category = "Protein" },
            new FoodItem { Name = "Food 2", Category = "Protein" },
            new FoodItem { Name = "Food 3", Category = "Carbs" }
        };
        await _context.FoodItems.AddRangeAsync(foodItems);
        await _context.SaveChangesAsync();

        var ids = new List<int> { foodItems[0].FoodItemId, foodItems[2].FoodItemId };

        // Act
        var result = await _repository.GetByIdsAsync(ids);

        // Assert
        result.Should().HaveCount(2);
        result.Select(f => f.Name).Should().BeEquivalentTo("Food 1", "Food 3");
    }
}

