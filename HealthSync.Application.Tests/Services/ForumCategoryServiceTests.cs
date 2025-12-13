using FluentAssertions;
using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class ForumCategoryServiceTests
{
    private readonly Mock<IForumCategoryRepository> _forumCategoryRepositoryMock;
    private readonly ForumCategoryService _service;

    public ForumCategoryServiceTests()
    {
        _forumCategoryRepositoryMock = new Mock<IForumCategoryRepository>();
        _service = new ForumCategoryService(_forumCategoryRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategoryAndReturnDto_WhenValidRequest()
    {
        // Arrange
        var request = new CreateForumCategoryRequest
        {
            Name = "General Discussion",
            Description = "General topics",
            DisplayOrder = 1
        };

        var createdEntity = new ForumCategory
        {
            CategoryId = 1,
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(false);

        _forumCategoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ForumCategory>()))
            .ReturnsAsync(createdEntity);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.DisplayOrder.Should().Be(request.DisplayOrder.Value);

        _forumCategoryRepositoryMock.Verify(r => r.ExistsByNameAsync(request.Name), Times.Once);
        _forumCategoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ForumCategory>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenNameAlreadyExists()
    {
        // Arrange
        var request = new CreateForumCategoryRequest
        {
            Name = "General Discussion",
            Description = "General topics"
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A category with this name already exists");

        _forumCategoryRepositoryMock.Verify(r => r.ExistsByNameAsync(request.Name), Times.Once);
        _forumCategoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ForumCategory>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseDefaultValues_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var request = new CreateForumCategoryRequest
        {
            Name = "Test Category",
            Description = null,
            DisplayOrder = null
        };

        var createdEntity = new ForumCategory
        {
            CategoryId = 1,
            Name = request.Name,
            Description = string.Empty,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(false);

        _forumCategoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ForumCategory>()))
            .ReturnsAsync(createdEntity);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeEmpty();
        result.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCategoryExists()
    {
        // Arrange
        var categoryId = 1;
        var entity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Test Category",
            Description = "Test Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(categoryId);
        result.Name.Should().Be(entity.Name);
        result.Description.Should().Be(entity.Description);
        result.DisplayOrder.Should().Be(entity.DisplayOrder);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = 999;

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((ForumCategory?)null);

        // Act
        var result = await _service.GetByIdAsync(categoryId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategoriesAsDtos()
    {
        // Arrange
        var entities = new List<ForumCategory>
        {
            new ForumCategory
            {
                CategoryId = 1,
                Name = "Category 1",
                Description = "Description 1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ForumCategory
            {
                CategoryId = 2,
                Name = "Category 2",
                Description = "Description 2",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategoryAndReturnDto_WhenValidRequest()
    {
        // Arrange
        var categoryId = 1;
        var existingEntity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Old Name",
            Description = "Old Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new UpdateForumCategoryRequest
        {
            Name = "New Name",
            Description = "New Description",
            DisplayOrder = 2
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingEntity);

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, categoryId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(categoryId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(categoryId);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.DisplayOrder.Should().Be(request.DisplayOrder.Value);

        _forumCategoryRepositoryMock.Verify(r => r.UpdateAsync(existingEntity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenCategoryNotFound()
    {
        // Arrange
        var categoryId = 999;
        var request = new UpdateForumCategoryRequest
        {
            Name = "New Name",
            Description = "New Description"
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((ForumCategory?)null);

        // Act
        var act = () => _service.UpdateAsync(categoryId, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Forum category not found");

        _forumCategoryRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _forumCategoryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ForumCategory>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenNameAlreadyExists()
    {
        // Arrange
        var categoryId = 1;
        var existingEntity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Old Name",
            Description = "Old Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateForumCategoryRequest
        {
            Name = "Existing Name",
            Description = "New Description"
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingEntity);

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(request.Name, categoryId))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.UpdateAsync(categoryId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Another category with this name already exists");

        _forumCategoryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ForumCategory>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenCategoryExistsAndNoRelatedPosts()
    {
        // Arrange
        var categoryId = 1;
        var entity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Test Category",
            Description = "Test Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(entity);

        _forumCategoryRepositoryMock
            .Setup(r => r.HasRelatedPostsAsync(categoryId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(categoryId);

        // Assert
        result.Should().BeTrue();

        _forumCategoryRepositoryMock.Verify(r => r.DeleteAsync(categoryId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = 999;

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((ForumCategory?)null);

        // Act
        var result = await _service.DeleteAsync(categoryId);

        // Assert
        result.Should().BeFalse();

        _forumCategoryRepositoryMock.Verify(r => r.HasRelatedPostsAsync(It.IsAny<int>()), Times.Never);
        _forumCategoryRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenCategoryHasRelatedPosts()
    {
        // Arrange
        var categoryId = 1;
        var entity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Test Category",
            Description = "Test Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(entity);

        _forumCategoryRepositoryMock
            .Setup(r => r.HasRelatedPostsAsync(categoryId))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.DeleteAsync(categoryId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete category with existing posts");

        _forumCategoryRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenNameExists()
    {
        // Arrange
        var name = "Test Category";

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(name))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsByNameAsync(name);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ShouldReturnTrue_WhenNameExistsForOtherCategory()
    {
        // Arrange
        var name = "Test Category";
        var excludeId = 1;

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsByNameAsync(name, excludeId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsByNameAsync(name, excludeId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasRelatedPostsAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var categoryId = 1;

        _forumCategoryRepositoryMock
            .Setup(r => r.HasRelatedPostsAsync(categoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasRelatedPostsAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var categoryId = 1;

        _forumCategoryRepositoryMock
            .Setup(r => r.ExistsAsync(categoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllOrderedByDisplayOrderAsync_ShouldReturnOrderedCategories()
    {
        // Arrange
        var entities = new List<ForumCategory>
        {
            new ForumCategory
            {
                CategoryId = 1,
                Name = "Category 1",
                Description = "Description 1",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ForumCategory
            {
                CategoryId = 2,
                Name = "Category 2",
                Description = "Description 2",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetAllOrderedAsync())
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllOrderedByDisplayOrderAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdWithPostsCountAsync_ShouldReturnDto_WhenCategoryExists()
    {
        // Arrange
        var categoryId = 1;
        var entity = new ForumCategory
        {
            CategoryId = categoryId,
            Name = "Test Category",
            Description = "Test Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdWithPostsAsync(categoryId))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdWithPostsCountAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(categoryId);
        result.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task GetByIdWithPostsCountAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = 999;

        _forumCategoryRepositoryMock
            .Setup(r => r.GetByIdWithPostsAsync(categoryId))
            .ReturnsAsync((ForumCategory?)null);

        // Act
        var result = await _service.GetByIdWithPostsCountAsync(categoryId);

        // Assert
        result.Should().BeNull();
    }
}