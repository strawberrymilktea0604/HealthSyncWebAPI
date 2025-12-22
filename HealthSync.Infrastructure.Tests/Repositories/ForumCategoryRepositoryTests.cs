using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ForumCategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ForumCategoryRepository _repository;

    public ForumCategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ForumCategoryRepository(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var categories = new List<ForumCategory>
        {
            new ForumCategory
            {
                CategoryId = 1,
                Name = "General Discussion",
                Description = "General forum discussions",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new ForumCategory
            {
                CategoryId = 2,
                Name = "Workout Tips",
                Description = "Share workout tips and advice",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new ForumCategory
            {
                CategoryId = 3,
                Name = "Nutrition Advice",
                Description = "Nutrition and diet discussions",
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.ForumCategories.AddRange(categories);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewCategoryAndReturnIt()
    {
        // Arrange
        var newCategory = new ForumCategory
        {
            Name = "New Category",
            Description = "A new forum category",
            DisplayOrder = 4
        };

        // Act
        var result = await _repository.AddAsync(newCategory);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Category");
        result.Description.Should().Be("A new forum category");
        result.DisplayOrder.Should().Be(4);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var savedCategory = await _context.ForumCategories.FindAsync(result.CategoryId);
        savedCategory.Should().NotBeNull();
        savedCategory!.Name.Should().Be("New Category");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
        result.Name.Should().Be("General Discussion");
        result.Description.Should().Be("General forum discussions");
        result.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategoriesOrderedByDisplayOrderThenName()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var categories = result.ToList();
        categories[0].DisplayOrder.Should().Be(1);
        categories[0].Name.Should().Be("General Discussion");
        categories[1].DisplayOrder.Should().Be(2);
        categories[1].Name.Should().Be("Workout Tips");
        categories[2].DisplayOrder.Should().Be(3);
        categories[2].Name.Should().Be("Nutrition Advice");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingCategory()
    {
        // Arrange
        var category = await _repository.GetByIdAsync(1);
        category!.Name = "Updated General Discussion";
        category.Description = "Updated description";
        category.DisplayOrder = 10;
        var originalUpdatedAt = category.UpdatedAt;

        // Act
        await _repository.UpdateAsync(category);

        // Assert
        var updatedCategory = await _repository.GetByIdAsync(1);
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Updated General Discussion");
        updatedCategory.Description.Should().Be("Updated description");
        updatedCategory.DisplayOrder.Should().Be(10);
        updatedCategory.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteExistingCategory()
    {
        // Act
        await _repository.DeleteAsync(1);

        // Assert
        var deletedCategory = await _repository.GetByIdAsync(1);
        deletedCategory.Should().BeNull();

        var allCategories = await _repository.GetAllAsync();
        allCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenCategoryDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteAsync(999);

        var allCategories = await _repository.GetAllAsync();
        allCategories.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenCategoryExists()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("General Discussion");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenNameMatchesCaseInsensitive()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("general discussion");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("Non-existent Category");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ShouldReturnTrue_WhenOtherCategoryHasSameName()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("General Discussion", 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ShouldReturnFalse_WhenOnlyExcludedCategoryHasName()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("General Discussion", 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasRelatedPostsAsync_ShouldReturnFalse_WhenCategoryHasNoPosts()
    {
        // Act
        var result = await _repository.HasRelatedPostsAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllOrderedAsync_ShouldReturnCategoriesOrderedByDisplayOrderThenName()
    {
        // Act
        var result = await _repository.GetAllOrderedAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var categories = result.ToList();
        categories[0].DisplayOrder.Should().Be(1);
        categories[0].Name.Should().Be("General Discussion");
        categories[1].DisplayOrder.Should().Be(2);
        categories[1].Name.Should().Be("Workout Tips");
        categories[2].DisplayOrder.Should().Be(3);
        categories[2].Name.Should().Be("Nutrition Advice");
    }

    [Fact]
    public async Task GetByIdWithPostsAsync_ShouldReturnCategoryWithPosts_WhenCategoryExists()
    {
        // Act
        var result = await _repository.GetByIdWithPostsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
        result.Name.Should().Be("General Discussion");
        result.Posts.Should().NotBeNull();
        // Note: In this test, Posts collection will be empty since we didn't seed posts
        result.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdWithPostsAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdWithPostsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenCategoryExists()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

