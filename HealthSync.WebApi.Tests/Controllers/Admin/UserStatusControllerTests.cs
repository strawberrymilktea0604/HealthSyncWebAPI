using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class UserStatusControllerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserStatusController>> _loggerMock;
    private readonly UserStatusController _controller;

    public UserStatusControllerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserStatusController>>();

        _controller = new UserStatusController(_userRepositoryMock.Object);

        // Setup admin claims
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task SetUserStatus_ReturnsNoContent_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var request = new SetActiveRequest(false);

        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "user@test.com",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.SetActiveStatusAsync(userId, false)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserStatus(userId, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _userRepositoryMock.Verify(x => x.SetActiveStatusAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task SetUserStatus_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var request = new SetActiveRequest(false);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.SetUserStatus(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var messageValue = messageProperty.GetValue(response);
        Assert.NotNull(messageValue);
        Assert.Equal("User not found", messageValue);
    }

    [Fact]
    public async Task SetUserRole_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var request = new SetRoleRequest("Admin");

        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "user@test.com",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRole(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(messageValue);

        Assert.True((bool)successValue);
        Assert.Equal("User role updated successfully", messageValue);

        // Verify that the user's role was updated
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.Role == "Admin")), Times.Once);
    }

    [Fact]
    public async Task SetUserRole_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var request = new SetRoleRequest("Admin");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.SetUserRole(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var messageProperty = responseType.GetProperty("message");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);

        var successValue = successProperty.GetValue(response);
        var messageValue = messageProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(messageValue);

        Assert.False((bool)successValue);
        Assert.Equal("User not found", messageValue);
    }
}

