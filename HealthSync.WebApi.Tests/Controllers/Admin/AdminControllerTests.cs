using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class AdminControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<AdminController>> _loggerMock;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<AdminController>>();

        _controller = new AdminController(
            _userServiceMock.Object,
            _loggerMock.Object
        );

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
    public async Task UpdateUserStatus_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserStatusRequest { IsActive = true };
        _userServiceMock.Setup(x => x.UpdateUserStatusAsync(userId, true)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUserStatus(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var jObject = JObject.Parse(json);
        Assert.True((bool)jObject["success"]!);
        Assert.Equal("User status updated successfully", (string)jObject["message"]!);
    }

    [Fact]
    public async Task UpdateUserStatus_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserStatusRequest { IsActive = true };
        _userServiceMock.Setup(x => x.UpdateUserStatusAsync(userId, true))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.UpdateUserStatus(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(notFoundResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("User not found", (string)jObject["message"]!);
    }

    [Fact]
    public async Task UpdateUserStatus_ReturnsInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserStatusRequest { IsActive = true };
        _userServiceMock.Setup(x => x.UpdateUserStatusAsync(userId, true))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateUserStatus(userId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(statusCodeResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("An error occurred", (string)jObject["message"]!);
        Assert.Equal("Database error", (string)jObject["error"]!);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRoleRequest { Role = "Admin" };
        _userServiceMock.Setup(x => x.UpdateUserRoleAsync(userId, "Admin")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUserRole(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var jObject = JObject.Parse(json);
        Assert.True((bool)jObject["success"]!);
        Assert.Equal("User role updated successfully", (string)jObject["message"]!);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRoleRequest { Role = "Admin" };
        _userServiceMock.Setup(x => x.UpdateUserRoleAsync(userId, "Admin"))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.UpdateUserRole(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(notFoundResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("User not found", (string)jObject["message"]!);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRoleRequest { Role = "Admin" };
        _userServiceMock.Setup(x => x.UpdateUserRoleAsync(userId, "Admin"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateUserRole(userId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(statusCodeResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("An error occurred", (string)jObject["message"]!);
        Assert.Equal("Database error", (string)jObject["error"]!);
    }

    [Fact]
    public async Task SetUserRankTitle_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var userId = 1;
        var request = new SetUserRankTitleRequest { RankTitle = "Top Contributor" };
        var expectedResult = new UserRankTitleDto { UserId = userId, RankTitle = "Top Contributor" };
        _userServiceMock.Setup(x => x.SetUserRankTitleAsync(userId, "Top Contributor"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SetUserRankTitle(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var jObject = JObject.Parse(json);
        Assert.True((bool)jObject["success"]!);
        Assert.Equal("User rank title updated successfully", (string)jObject["message"]!);
        var data = jObject["data"]!.ToObject<UserRankTitleDto>()!;
        Assert.Equal(expectedResult.UserId, data.UserId);
        Assert.Equal(expectedResult.RankTitle, data.RankTitle);
    }

    [Fact]
    public async Task SetUserRankTitle_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = 1;
        var request = new SetUserRankTitleRequest { RankTitle = "Top Contributor" };
        _userServiceMock.Setup(x => x.SetUserRankTitleAsync(userId, "Top Contributor"))
            .ReturnsAsync((UserRankTitleDto?)null);

        // Act
        var result = await _controller.SetUserRankTitle(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(notFoundResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("User not found", (string)jObject["message"]!);
    }

    [Fact]
    public async Task SetUserRankTitle_ReturnsInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = 1;
        var request = new SetUserRankTitleRequest { RankTitle = "Top Contributor" };
        _userServiceMock.Setup(x => x.SetUserRankTitleAsync(userId, "Top Contributor"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SetUserRankTitle(userId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(statusCodeResult.Value);
        var jObject = JObject.Parse(json);
        Assert.False((bool)jObject["success"]!);
        Assert.Equal("An error occurred while updating user rank title", (string)jObject["message"]!);
        Assert.Equal("Database error", (string)jObject["error"]!);
    }
}