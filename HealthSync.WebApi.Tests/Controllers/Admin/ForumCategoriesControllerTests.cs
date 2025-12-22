using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class ForumCategoriesControllerTests
{
    private readonly Mock<IForumCategoryService> _mockForumCategoryService;
    private readonly Mock<ILogger<ForumCategoriesController>> _mockLogger;
    private readonly ForumCategoriesController _controller;

    public ForumCategoriesControllerTests()
    {
        _mockForumCategoryService = new Mock<IForumCategoryService>();
        _mockLogger = new Mock<ILogger<ForumCategoriesController>>();
        _controller = new ForumCategoriesController(_mockForumCategoryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllCategories_ReturnsOkWithCategories()
    {
        // Arrange
        var categories = new List<ForumCategoryDto>
        {
            new ForumCategoryDto { Id = 1, Name = "General", DisplayOrder = 1 },
            new ForumCategoryDto { Id = 2, Name = "Nutrition", DisplayOrder = 2 }
        };
        _mockForumCategoryService.Setup(s => s.GetAllOrderedByDisplayOrderAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAllCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCategories = Assert.IsAssignableFrom<IEnumerable<ForumCategoryDto>>(okResult.Value);
        Assert.Equal(2, returnedCategories.Count());
    }

    [Fact]
    public async Task GetAllCategories_ServiceThrowsException_Returns500()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.GetAllOrderedByDisplayOrderAsync()).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.GetAllCategories();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetCategoryById_ExistingId_ReturnsOk()
    {
        // Arrange
        var category = new ForumCategoryDto { Id = 1, Name = "General" };
        _mockForumCategoryService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        var result = await _controller.GetCategoryById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCategory = Assert.IsType<ForumCategoryDto>(okResult.Value);
        Assert.Equal(1, returnedCategory.Id);
    }

    [Fact]
    public async Task GetCategoryById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ForumCategoryDto?)null);

        // Act
        var result = await _controller.GetCategoryById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateForumCategoryRequest { Name = "New Category", Description = "Test", DisplayOrder = 1 };
        var createdCategory = new ForumCategoryDto { Id = 1, Name = "New Category" };
        _mockForumCategoryService.Setup(s => s.CreateAsync(request)).ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.CreateCategory(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ForumCategoriesController.GetCategoryById), createdResult.ActionName);
        var returnedCategory = Assert.IsType<ForumCategoryDto>(createdResult.Value);
        Assert.Equal(1, returnedCategory.Id);
    }

    [Fact]
    public async Task CreateCategory_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateForumCategoryRequest { Name = "Duplicate", Description = "Test", DisplayOrder = 1 };
        _mockForumCategoryService.Setup(s => s.CreateAsync(request)).ThrowsAsync(new InvalidOperationException("Category already exists"));

        // Act
        var result = await _controller.CreateCategory(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCategory_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest { Name = "Updated Category", Description = "Updated", DisplayOrder = 2 };
        var updatedCategory = new ForumCategoryDto { Id = 1, Name = "Updated Category" };
        _mockForumCategoryService.Setup(s => s.UpdateAsync(1, request)).ReturnsAsync(updatedCategory);

        // Act
        var result = await _controller.UpdateCategory(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCategory = Assert.IsType<ForumCategoryDto>(okResult.Value);
        Assert.Equal("Updated Category", returnedCategory.Name);
    }

    [Fact]
    public async Task UpdateCategory_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest { Name = "Updated", Description = "Test", DisplayOrder = 1 };
        _mockForumCategoryService.Setup(s => s.UpdateAsync(999, request)).ThrowsAsync(new KeyNotFoundException("Category not found"));

        // Act
        var result = await _controller.UpdateCategory(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteCategory_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_CategoryHasPosts_ReturnsConflict()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.DeleteAsync(1)).ThrowsAsync(new InvalidOperationException("Cannot delete category with posts"));

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CategoryExists_ExistingId_ReturnsTrue()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.ExistsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.CategoryExists(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task HasRelatedPosts_CategoryWithPosts_ReturnsTrue()
    {
        // Arrange
        _mockForumCategoryService.Setup(s => s.HasRelatedPostsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.HasRelatedPosts(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }
}
