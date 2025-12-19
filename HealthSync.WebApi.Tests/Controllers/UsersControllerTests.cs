using FluentAssertions;
using System.Text.Json;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserProfileService> _mockProfileService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockProfileService = new Mock<IUserProfileService>();
        _mockFileStorageService = new Mock<IFileStorageService>();

        _controller = new UsersController(_mockProfileService.Object, _mockFileStorageService.Object);

        // Setup authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnOk_WhenProfileExists()
    {
        // Arrange
        var userId = 1;
        var expectedProfile = new UserProfileResponse(
            UserProfileId: userId,
            FullName: "Test User",
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: "Male",
            HeightCm: 175m,
            CurrentWeightKg: 70m,
            ActivityLevel: "ModeratelyActive",
            AvatarUrl: "https://example.com/avatar.jpg",
            ContributionPoints: 100,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

        _mockProfileService
            .Setup(s => s.GetUserProfileResponseAsync(userId))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<ApiResponse<UserProfileResponse>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.FullName.Should().Be("Test User");
        response.Data.UserProfileId.Should().Be(userId);
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnNotFound_WhenProfileNotExists()
    {
        // Arrange
        var userId = 1;

        _mockProfileService
            .Setup(s => s.GetUserProfileResponseAsync(userId))
            .ReturnsAsync((UserProfileResponse?)null);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        var response = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("Profile not found");
    }

    [Fact]
    public async Task UpdateMyProfile_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserProfileRequest(
            FullName: "Updated Name",
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: "Male",
            HeightCm: 180m,
            CurrentWeightKg: 75m,
            ActivityLevel: "VeryActive",
            AvatarUrl: null
        );

        var updatedProfile = new UserProfileDto(
            UserProfileId: userId,
            UserId: userId,
            FullName: "Updated Name",
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: "Male",
            HeightCm: 180m,
            CurrentWeightKg: 75m,
            ActivityLevel: "VeryActive",
            AvatarUrl: null,
            ContributionPoints: 100,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _mockProfileService
            .Setup(s => s.UpdateUserProfileAsync(request, userId))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateMyProfile(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("FullName").GetString().Should().Be("Updated Name");
        data.GetProperty("HeightCm").GetDecimal().Should().Be(180);
    }

    [Fact]
    public async Task UpdateMyProfile_ShouldReturnBadRequest_WhenModelInvalid()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "", // Invalid: empty name
            DateOfBirth: new DateTime(1990, 1, 1),
            Gender: "Male",
            HeightCm: 180m,
            CurrentWeightKg: 75m,
            ActivityLevel: "VeryActive",
            AvatarUrl: null
        );

        _controller.ModelState.AddModelError("FullName", "FullName is required");

        // Act
        var result = await _controller.UpdateMyProfile(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var response = JsonSerializer.Deserialize<JsonElement>(json);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnOk_WhenUploadSucceeds()
    {
        // Arrange
        var userId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024); // 1KB file
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var uploadedUrl = "https://minio.example.com/avatars/avatar.jpg";

        _mockProfileService
            .Setup(s => s.GetUserProfileAsync(userId))
            .ReturnsAsync(new UserProfileDto(
                UserProfileId: userId,
                UserId: userId,
                FullName: "Test User",
                Gender: "Male",
                DateOfBirth: new DateTime(1990, 1, 1),
                HeightCm: 175m,
                CurrentWeightKg: 70m,
                ActivityLevel: "ModeratelyActive",
                AvatarUrl: null,
                ContributionPoints: 100,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null
            ));

        _mockFileStorageService
            .Setup(s => s.UploadAsync(fileMock.Object, "avatars"))
            .ReturnsAsync(uploadedUrl);

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("avatarUrl").GetString().Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnBadRequest_WhenNoFileUploaded()
    {
        // Act
        var result = await _controller.UploadAvatar(null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var response = JsonSerializer.Deserialize<JsonElement>(json);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("No file uploaded");
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnBadRequest_WhenFileTooLarge()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10MB file

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var response = JsonSerializer.Deserialize<JsonElement>(json);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("File size must be less than 5MB");
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnBadRequest_WhenInvalidFileType()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var response = JsonSerializer.Deserialize<JsonElement>(json);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("Only JPEG and PNG images are allowed");
    }

    [Fact]
    public async Task GetMyStats_ShouldReturnOk_WhenStatsRetrieved()
    {
        // Arrange
        var userId = 1;
        var expectedStats = new UserStatsDto(
            TotalWorkouts: 10,
            TotalNutritionLogs: 20,
            TotalGoals: 5,
            TotalChallenges: 3,
            ContributionPoints: 150
        );

        _mockProfileService
            .Setup(s => s.GetUserStatsAsync(userId))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetMyStats();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("TotalWorkouts").GetInt32().Should().Be(10);
        data.GetProperty("TotalNutritionLogs").GetInt32().Should().Be(20);
        data.GetProperty("TotalGoals").GetInt32().Should().Be(5);
        data.GetProperty("TotalChallenges").GetInt32().Should().Be(3);
        data.GetProperty("ContributionPoints").GetInt32().Should().Be(150);
    }
}