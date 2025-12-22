using FluentAssertions;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class FoodsControllerTests
{
    private readonly Mock<IFoodItemService> _mockFoodItemService;
    private readonly Mock<ILogger<FoodsController>> _mockLogger;
    private readonly FoodsController _controller;

    public FoodsControllerTests()
    {
        _mockFoodItemService = new Mock<IFoodItemService>();
        _mockLogger = new Mock<ILogger<FoodsController>>();
        _controller = new FoodsController(_mockFoodItemService.Object, _mockLogger.Object);
    }

    private void SetupUserClaims(int userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Search_ShouldReturnOk_WithResults()
    {
        // Arrange
        var searchTerm = "chicken";
        var category = "Protein";
        var expectedResult = new PaginatedResult<FoodItemDto>
        {
            Items = new List<FoodItemDto>
            {
                new FoodItemDto { FoodItemId = 1, Name = "Grilled Chicken", Category = "Protein" }
            },
            TotalItems = 1,
            CurrentPage = 1,
            PageSize = 100,
            TotalPages = 1
        };

        _mockFoodItemService
            .Setup(s => s.SearchAsync(searchTerm, category, 1, 100))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Search(searchTerm, category);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = expectedResult,
            message = "Food items retrieved successfully"
        });
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenSearchTermTooLong()
    {
        // Arrange
        var longSearchTerm = new string('a', 201);

        // Act
        var result = await _controller.Search(longSearchTerm, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        var value = badRequestResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Search term must not exceed 200 characters"
        });
    }

    [Fact]
    public async Task Search_ShouldTrimSearchTerm()
    {
        // Arrange
        var searchTerm = "  chicken  ";
        var expectedResult = new PaginatedResult<FoodItemDto>
        {
            Items = new List<FoodItemDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            PageSize = 100,
            TotalPages = 0
        };

        _mockFoodItemService
            .Setup(s => s.SearchAsync("chicken", null, 1, 100))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Search(searchTerm, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = expectedResult
        });
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenFoodItemExists()
    {
        // Arrange
        var foodItem = new FoodItemDto
        {
            FoodItemId = 1,
            Name = "Grilled Chicken",
            Category = "Protein",
            CaloriesPerServing = 165
        };

        _mockFoodItemService
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(foodItem);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = foodItem,
            message = "Food item retrieved successfully"
        });
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenFoodItemDoesNotExist()
    {
        // Arrange
        _mockFoodItemService
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.StatusCode.Should().Be(404);
        var response = notFoundResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Food item not found");
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        var request = new CreateFoodItemRequest
        {
            Name = "New Food Item",
            Category = "Protein",
            CaloriesPerServing = 200
        };

        var createdFoodItem = new FoodItemDto
        {
            FoodItemId = 1,
            Name = request.Name,
            Category = request.Category,
            CaloriesPerServing = request.CaloriesPerServing
        };

        _mockFoodItemService
            .Setup(s => s.CreateAsync(request))
            .ReturnsAsync(createdFoodItem);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);

        var response = createdResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var dataProperty = response.GetType().GetProperty("data")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(true);
        dataProperty.Should().Be(createdFoodItem);
        messageProperty.Should().Be("Food item created");
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        _controller.ModelState.AddModelError("Name", "Required");

        var request = new CreateFoodItemRequest();

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        // BadRequest(ModelState) serializes to SerializableError (Dictionary<string, string[]>)
        var serializedErrors = badRequestResult.Value as Microsoft.AspNetCore.Mvc.SerializableError;
        serializedErrors.Should().NotBeNull();
        serializedErrors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        var request = new UpdateFoodItemRequest
        {
            Name = "Updated Food Item",
            CaloriesPerServing = 250
        };

        var updatedFoodItem = new FoodItemDto
        {
            FoodItemId = 1,
            Name = request.Name,
            CaloriesPerServing = request.CaloriesPerServing
        };

        _mockFoodItemService
            .Setup(s => s.UpdateAsync(1, request))
            .ReturnsAsync(updatedFoodItem);

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = updatedFoodItem,
            message = "Food item updated"
        });
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenFoodItemDoesNotExist()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        var request = new UpdateFoodItemRequest { Name = "Updated Name" };

        _mockFoodItemService
            .Setup(s => s.UpdateAsync(1, request))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = (FoodItemDto?)null,
            message = "Food item updated"
        });
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDeleteSucceeds()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        _mockFoodItemService
            .Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenFoodItemDoesNotExist()
    {
        // Arrange
        SetupUserClaims(1, "Admin");
        _mockFoodItemService
            .Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.StatusCode.Should().Be(404);
        var response = notFoundResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("Food item not found or cannot be deleted");
    }

    [Fact]
    public async Task Search_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        _mockFoodItemService
            .Setup(s => s.SearchAsync(null, null, 1, 100))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Search(null, null);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = (ObjectResult)result;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("An error occurred while searching food items");
    }
}