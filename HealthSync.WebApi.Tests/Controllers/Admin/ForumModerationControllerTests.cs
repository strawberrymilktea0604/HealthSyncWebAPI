using System.Security.Claims;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class ForumModerationControllerTests
{
    private readonly Mock<IForumAdminService> _mockForumAdminService;
    private readonly Mock<ILogger<ForumModerationController>> _mockLogger;
    private readonly ForumModerationController _controller;

    public ForumModerationControllerTests()
    {
        _mockForumAdminService = new Mock<IForumAdminService>();
        _mockLogger = new Mock<ILogger<ForumModerationController>>();
        _controller = new ForumModerationController(_mockForumAdminService.Object, _mockLogger.Object);
        
        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task PinPost_ValidPost_ReturnsOk()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.PinPostAsync(1, 1)).ReturnsAsync((true, "Post pinned successfully"));

        // Act
        var result = await _controller.PinPost(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PinPost_PostNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.PinPostAsync(999, 1)).ReturnsAsync((false, "Post not found"));

        // Act
        var result = await _controller.PinPost(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PinPost_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.PinPost(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PinPost_ServiceThrowsException_Returns500()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.PinPostAsync(1, 1)).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.PinPost(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task LockPost_ValidPost_ReturnsOk()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.LockPostAsync(1, 1)).ReturnsAsync((true, "Post locked successfully"));

        // Act
        var result = await _controller.LockPost(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task LockPost_PostNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.LockPostAsync(999, 1)).ReturnsAsync((false, "Post not found"));

        // Act
        var result = await _controller.LockPost(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task LockPost_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.LockPost(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeletePost_ValidPost_ReturnsOk()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.DeletePostAsync(1, 1)).ReturnsAsync((true, "Post deleted successfully"));

        // Act
        var result = await _controller.DeletePost(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeletePost_PostNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.DeletePostAsync(999, 1)).ReturnsAsync((false, "Post not found"));

        // Act
        var result = await _controller.DeletePost(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePost_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.DeletePost(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task HideReply_ValidReply_ReturnsOk()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.HideReplyAsync(1, 1)).ReturnsAsync((true, "Reply hidden successfully"));

        // Act
        var result = await _controller.HideReply(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task HideReply_ReplyNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.HideReplyAsync(999, 1)).ReturnsAsync((false, "Reply not found"));

        // Act
        var result = await _controller.HideReply(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task HideReply_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.HideReply(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReply_ValidReply_ReturnsOk()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.DeleteReplyAsync(1, 1)).ReturnsAsync((true, "Reply deleted successfully"));

        // Act
        var result = await _controller.DeleteReply(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteReply_ReplyNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.DeleteReplyAsync(999, 1)).ReturnsAsync((false, "Reply not found"));

        // Act
        var result = await _controller.DeleteReply(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReply_InvalidAdminId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.DeleteReply(1);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReply_ServiceThrowsException_Returns500()
    {
        // Arrange
        _mockForumAdminService.Setup(s => s.DeleteReplyAsync(1, 1)).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.DeleteReply(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }
}
