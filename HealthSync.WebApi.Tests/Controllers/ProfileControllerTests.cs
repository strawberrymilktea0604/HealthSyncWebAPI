using FluentAssertions;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _mockStorageService = new Mock<IStorageService>();
        _mockUserProfileRepository = new Mock<IUserProfileRepository>();

        _controller = new ProfileController(_mockStorageService.Object, _mockUserProfileRepository.Object);
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnBadRequest_WhenNoFileUploaded()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        var type = badRequestResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(badRequestResult.Value);
        messageValue.Should().Be("No file uploaded");
    }

    [Fact]
    public async Task UploadAvatar_ShouldReturnUnauthorized_WhenInvalidUserId()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
        var type = unauthorizedResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
        messageValue.Should().Be("Invalid user");
    }

    [Fact]
    public async Task UploadAvatar_ShouldCreateNewProfile_WhenProfileDoesNotExist()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var userId = 1;
        var expectedUrl = "https://storage.example.com/avatars/avatar_1.jpg";

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockStorageService
            .Setup(s => s.UploadAsync(fileMock.Object, "avatars", "avatar_1.jpg"))
            .ReturnsAsync(expectedUrl);

        _mockUserProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(default(UserProfile));

        _mockUserProfileRepository
            .Setup(r => r.AddAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var type = okResult.Value.GetType();
        var avatarUrlProperty = type.GetProperty("avatarUrl");
        avatarUrlProperty.Should().NotBeNull();
        var avatarUrlValue = avatarUrlProperty.GetValue(okResult.Value);
        avatarUrlValue.Should().Be(expectedUrl);

        _mockUserProfileRepository.Verify(r => r.AddAsync(It.Is<UserProfile>(p =>
            p.UserId == userId &&
            p.AvatarUrl == expectedUrl)), Times.Once);
    }

    [Fact]
    public async Task UploadAvatar_ShouldUpdateExistingProfile_WhenProfileExists()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.png");

        var userId = 2;
        var expectedUrl = "https://storage.example.com/avatars/avatar_2.png";
        var existingProfile = new UserProfile
        {
            UserId = userId,
            FullName = "Existing User",
            AvatarUrl = "old_url.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockStorageService
            .Setup(s => s.UploadAsync(fileMock.Object, "avatars", "avatar_2.png"))
            .ReturnsAsync(expectedUrl);

        _mockUserProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingProfile);

        _mockUserProfileRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UploadAvatar(fileMock.Object);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var type = okResult.Value.GetType();
        var avatarUrlProperty = type.GetProperty("avatarUrl");
        avatarUrlProperty.Should().NotBeNull();
        var avatarUrlValue = avatarUrlProperty.GetValue(okResult.Value);
        avatarUrlValue.Should().Be(expectedUrl);

        _mockUserProfileRepository.Verify(r => r.UpdateAsync(It.Is<UserProfile>(p =>
            p.UserId == userId &&
            p.AvatarUrl == expectedUrl)), Times.Once);
    }
}