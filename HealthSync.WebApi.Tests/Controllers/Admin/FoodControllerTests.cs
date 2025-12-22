using System.Security.Claims;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class FoodControllerTests
{
    private readonly Mock<IFoodItemService> _foodItemServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ILogger<FoodController>> _loggerMock;
    private readonly FoodController _controller;
    private readonly ClaimsPrincipal _adminUser;

    public FoodControllerTests()
    {
        _foodItemServiceMock = new Mock<IFoodItemService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<FoodController>>();

        _controller = new FoodController(
            _foodItemServiceMock.Object,
            _fileStorageServiceMock.Object,
            _loggerMock.Object);

        // Setup admin user claims
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithFoodItems()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<FoodItemDto>
        {
            Items = new List<FoodItemDto>
            {
                new FoodItemDto { FoodItemId = 1, Name = "Chicken Breast", Category = "Protein" },
                new FoodItemDto { FoodItemId = 2, Name = "Brown Rice", Category = "Carbs" }
            },
            CurrentPage = 1,
            PageSize = 20,
            TotalItems = 2,
            TotalPages = 1,
            HasNext = false
        };

        _foodItemServiceMock.Setup(x => x.SearchAsync(null, null, 1, 20))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetAll(null, null, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PaginatedResult<FoodItemDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count);
        Assert.Equal(1, returnedResult.CurrentPage);
        Assert.Equal(20, returnedResult.PageSize);
    }

    [Fact]
    public async Task GetAll_WithSearchParameters_ReturnsFilteredResults()
    {
        // Arrange
        var searchTerm = "chicken";
        var category = "Protein";
        var paginatedResult = new PaginatedResult<FoodItemDto>
        {
            Items = new List<FoodItemDto>
            {
                new FoodItemDto { FoodItemId = 1, Name = "Chicken Breast", Category = "Protein" }
            },
            CurrentPage = 1,
            PageSize = 20,
            TotalItems = 1,
            TotalPages = 1,
            HasNext = false
        };

        _foodItemServiceMock.Setup(x => x.SearchAsync(searchTerm, category, 1, 20))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetAll(searchTerm, category, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PaginatedResult<FoodItemDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
        Assert.Equal("Chicken Breast", returnedResult.Items.First().Name);
    }

    [Fact]
    public async Task GetById_ExistingFoodItem_ReturnsOkResult()
    {
        // Arrange
        var foodItemId = 1;
        var foodItem = new FoodItemDto
        {
            FoodItemId = foodItemId,
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6M
        };

        _foodItemServiceMock.Setup(x => x.GetByIdAsync(foodItemId))
            .ReturnsAsync(foodItem);

        // Act
        var result = await _controller.GetById(foodItemId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedFoodItem = Assert.IsType<FoodItemDto>(okResult.Value);
        Assert.Equal(foodItemId, returnedFoodItem.FoodItemId);
        Assert.Equal("Chicken Breast", returnedFoodItem.Name);
    }

    [Fact]
    public async Task GetById_NonExistingFoodItem_ReturnsNotFound()
    {
        // Arrange
        var foodItemId = 999;
        _foodItemServiceMock.Setup(x => x.GetByIdAsync(foodItemId))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _controller.GetById(foodItemId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value; Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var message = (string)messageProperty.GetValue(response)!;
        Assert.NotNull(message);
        Assert.Contains($"Food item with ID {foodItemId} not found", message!);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6M,
            Description = "Lean protein source"
        };

        var createdFoodItem = new FoodItemDto
        {
            FoodItemId = 1,
            Name = request.Name,
            Category = request.Category,
            ServingSize = request.ServingSize,
            ServingUnit = request.ServingUnit,
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            Description = request.Description
        };

        _foodItemServiceMock.Setup(x => x.CreateAsync(request))
            .ReturnsAsync(createdFoodItem);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        Assert.Equal(createdFoodItem.FoodItemId, createdResult.RouteValues!["id"]);

        var returnedFoodItem = Assert.IsType<FoodItemDto>(createdResult.Value);
        Assert.Equal(createdFoodItem.FoodItemId, returnedFoodItem.FoodItemId);
        Assert.Equal(request.Name, returnedFoodItem.Name);
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Name is required");

        var request = new CreateFoodItemRequest(); // Empty request

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6M
        };

        _foodItemServiceMock.Setup(x => x.CreateAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while creating the food item", statusCodeResult.Value);
    }

    [Fact]
    public async Task Update_ExistingFoodItem_ReturnsOkResult()
    {
        // Arrange
        var foodItemId = 1;
        var request = new UpdateFoodItemRequest
        {
            Name = "Updated Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 170,
            ProteinG = 32,
            CarbsG = 0,
            FatG = 3.8M,
            Description = "Updated description"
        };

        var updatedFoodItem = new FoodItemDto
        {
            FoodItemId = foodItemId,
            Name = request.Name,
            Category = request.Category,
            ServingSize = request.ServingSize,
            ServingUnit = request.ServingUnit,
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            Description = request.Description
        };

        _foodItemServiceMock.Setup(x => x.UpdateAsync(foodItemId, request))
            .ReturnsAsync(updatedFoodItem);

        // Act
        var result = await _controller.Update(foodItemId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedFoodItem = Assert.IsType<FoodItemDto>(okResult.Value);
        Assert.Equal(foodItemId, returnedFoodItem.FoodItemId);
        Assert.Equal(request.Name, returnedFoodItem.Name);
    }

    [Fact]
    public async Task Update_NonExistingFoodItem_ReturnsNotFound()
    {
        // Arrange
        var foodItemId = 999;
        var request = new UpdateFoodItemRequest
        {
            Name = "Updated Food",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 100,
            ProteinG = 20,
            CarbsG = 0,
            FatG = 2
        };

        _foodItemServiceMock.Setup(x => x.UpdateAsync(foodItemId, request))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _controller.Update(foodItemId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value; Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var message = (string)messageProperty.GetValue(response)!;
        Assert.NotNull(message);
        Assert.Contains($"Food item with ID {foodItemId} not found", message!);
    }

    [Fact]
    public async Task Update_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Name is required");

        var request = new UpdateFoodItemRequest(); // Empty request

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadImage_ValidFile_ReturnsOkResult()
    {
        // Arrange
        var foodItemId = 1;
        var fileMock = new Mock<IFormFile>();
        var content = "Fake image content";
        var fileName = "test.jpg";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        var existingFoodItem = new FoodItemDto
        {
            FoodItemId = foodItemId,
            Name = "Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "g",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6M
        };

        var imageUrl = "https://minio.example.com/foods/test.jpg";

        _foodItemServiceMock.Setup(x => x.GetByIdAsync(foodItemId))
            .ReturnsAsync(existingFoodItem);
        _fileStorageServiceMock.Setup(x => x.UploadAsync(fileMock.Object, "foods"))
            .ReturnsAsync(imageUrl);
        _foodItemServiceMock.Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateFoodItemRequest>()))
            .ReturnsAsync(existingFoodItem);

        // Act
        var result = await _controller.UploadImage(foodItemId, fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var imageUrlProperty = responseType.GetProperty("imageUrl");
        Assert.NotNull(imageUrlProperty);

        var returnedImageUrl = (string)imageUrlProperty.GetValue(response)!;
        Assert.NotNull(returnedImageUrl);
        Assert.Equal(imageUrl, returnedImageUrl!);
    }

    [Fact]
    public async Task UploadImage_NoFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadImage(1, (IFormFile)null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadImage_InvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.ContentType).Returns("text/plain");

        // Act
        var result = await _controller.UploadImage(1, fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid file type. Only JPEG and PNG are allowed.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadImage_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6MB
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        // Act
        var result = await _controller.UploadImage(1, fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File size exceeds 5MB limit.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadImage_FoodItemNotFound_ReturnsNotFound()
    {
        // Arrange
        var foodItemId = 999;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        _foodItemServiceMock.Setup(x => x.GetByIdAsync(foodItemId))
            .ReturnsAsync((FoodItemDto?)null);

        // Act
        var result = await _controller.UploadImage(foodItemId, fileMock.Object);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value; Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var message = (string)messageProperty.GetValue(response)!;
        Assert.NotNull(message);
        Assert.Contains($"Food item with ID {foodItemId} not found", message!);
    }

    [Fact]
    public async Task Delete_ExistingFoodItem_ReturnsNoContent()
    {
        // Arrange
        var foodItemId = 1;
        _foodItemServiceMock.Setup(x => x.DeleteAsync(foodItemId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(foodItemId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingFoodItem_ReturnsNotFound()
    {
        // Arrange
        var foodItemId = 999;
        _foodItemServiceMock.Setup(x => x.DeleteAsync(foodItemId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(foodItemId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value; Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var message = (string)messageProperty.GetValue(response)!;
        Assert.NotNull(message);
        Assert.Contains($"Food item with ID {foodItemId} not found", message!);
    }

    [Fact]
    public async Task Delete_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var foodItemId = 1;
        _foodItemServiceMock.Setup(x => x.DeleteAsync(foodItemId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Delete(foodItemId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while deleting the food item", statusCodeResult.Value);
    }
}

