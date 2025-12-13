using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class FoodItemServiceTests
{
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly FoodItemService _service;

    public FoodItemServiceTests()
    {
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _service = new FoodItemService(
            _foodItemRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var expectedResult = new PaginatedResult<FoodItemDto>
        {
            Items = new List<FoodItemDto>
            {
                new FoodItemDto
                {
                    FoodItemId = 1,
                    Name = "Chicken Breast",
                    Category = "Protein",
                    CaloriesPerServing = 165m
                }
            },
            TotalItems = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _foodItemRepositoryMock
            .Setup(r => r.SearchAsync("chicken", "Protein", 1, 20))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchAsync("chicken", "Protein", 1, 20);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFoodItemDto_WhenExists()
    {
        // Arrange
        var foodItemDto = new FoodItemDto
        {
            FoodItemId = 1,
            Name = "Chicken Breast",
            Category = "Protein",
            CaloriesPerServing = 165m,
            ProteinG = 31m,
            CarbsG = 0m,
            FatG = 3.6m
        };

        _foodItemRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(foodItemDto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().BeEquivalentTo(foodItemDto);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _foodItemRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateFoodItemAndReturnDto_WhenValidUser()
    {
        // Arrange
        var userId = 1;
        var request = new CreateFoodItemRequest
        {
            Name = "Chicken Breast",
            Category = "Protein",
            Description = "Lean protein source",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165m,
            ProteinG = 31m,
            CarbsG = 0m,
            FatG = 3.6m
        };

        var user = new ApplicationUser { UserId = userId, Email = "admin@example.com" };
        var createdFoodItem = new FoodItem
        {
            FoodItemId = 1,
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            ServingSize = request.ServingSize,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            CreatedByAdminId = userId
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString())
        }));

        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns(httpContext);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _foodItemRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<FoodItem>()))
            .ReturnsAsync(createdFoodItem);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FoodItemId.Should().Be(1);
        result.Name.Should().Be("Chicken Breast");
        result.Category.Should().Be("Protein");
        result.CaloriesPerServing.Should().Be(165);
        result.ProteinG.Should().Be(31);

        _foodItemRepositoryMock.Verify(r => r.CreateAsync(It.Is<FoodItem>(f =>
            f.Name == request.Name &&
            f.CreatedByAdminId == userId)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenNoUserIdInToken()
    {
        // Arrange
        var request = new CreateFoodItemRequest { Name = "Chicken Breast" };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns(httpContext);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenInvalidUserIdInToken()
    {
        // Arrange
        var request = new CreateFoodItemRequest { Name = "Chicken Breast" };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "invalid-id")
        }));

        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns(httpContext);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        var request = new CreateFoodItemRequest { Name = "Chicken Breast" };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString())
        }));

        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns(httpContext);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFoodItemAndReturnDto_WhenExists()
    {
        // Arrange
        var foodItemId = 1;
        var request = new UpdateFoodItemRequest
        {
            Name = "Updated Chicken Breast",
            Category = "Protein",
            Description = "Updated description",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 170m,
            ProteinG = 32m,
            CarbsG = 0m,
            FatG = 4.0m
        };

        var updatedFoodItem = new FoodItem
        {
            FoodItemId = foodItemId,
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            ServingSize = request.ServingSize,
            ServingUnit = ServingUnit.Gram,
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG
        };

        _foodItemRepositoryMock
            .Setup(r => r.UpdateAsync(foodItemId, It.IsAny<FoodItem>()))
            .ReturnsAsync(updatedFoodItem);

        // Act
        var result = await _service.UpdateAsync(foodItemId, request);

        // Assert
        result.Should().NotBeNull();
        result!.FoodItemId.Should().Be(foodItemId);
        result.Name.Should().Be("Updated Chicken Breast");
        result.CaloriesPerServing.Should().Be(170);
        result.ProteinG.Should().Be(32);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenFoodItemNotFound()
    {
        // Arrange
        var request = new UpdateFoodItemRequest
        {
            Name = "Updated Name",
            ServingUnit = "Gram"
        };

        _foodItemRepositoryMock
            .Setup(r => r.UpdateAsync(999, It.IsAny<FoodItem>()))
            .ReturnsAsync((FoodItem?)null);

        // Act
        var result = await _service.UpdateAsync(999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenDeletionSuccessful()
    {
        // Arrange
        _foodItemRepositoryMock
            .Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenDeletionFails()
    {
        // Arrange
        _foodItemRepositoryMock
            .Setup(r => r.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}