using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class UserTitleControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserTitleController _controller;

    public UserTitleControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UserTitleController(_mockUserService.Object);
    }

    [Fact]
    public async Task SetUserTitle_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new SetRankTitleRequest("Top Contributor");
        var result = new UserRankTitleDto { UserId = 1, RankTitle = "Top Contributor" };
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(1, "Top Contributor")).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.SetUserTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(1, "Top Contributor"), Times.Once);
    }

    [Fact]
    public async Task SetUserTitle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new SetRankTitleRequest("Top Contributor");
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(999, "Top Contributor")).ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var actionResult = await _controller.SetUserTitle(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task SetUserTitle_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var request = new SetRankTitleRequest("Invalid Title");
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(1, "Invalid Title")).ThrowsAsync(new Exception("Invalid operation"));

        // Act
        var actionResult = await _controller.SetUserTitle(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task SetUserTitle_NullRankTitle_CallsServiceWithNull()
    {
        // Arrange
        var request = new SetRankTitleRequest(null);
        var result = new UserRankTitleDto { UserId = 1 };
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(1, null)).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.SetUserTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(1, null), Times.Once);
    }

    [Fact]
    public async Task SetUserTitle_EmptyRankTitle_CallsServiceWithEmpty()
    {
        // Arrange
        var request = new SetRankTitleRequest("");
        var result = new UserRankTitleDto { UserId = 1 };
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(1, "")).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.SetUserTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(1, ""), Times.Once);
    }

    [Fact]
    public async Task SetUserTitle_LongRankTitle_ReturnsOk()
    {
        // Arrange
        var longTitle = new string('A', 100);
        var request = new SetRankTitleRequest(longTitle);
        var result = new UserRankTitleDto { UserId = 1 };
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(1, longTitle)).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.SetUserTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(1, longTitle), Times.Once);
    }

    [Fact]
    public async Task SetUserTitle_MultipleUsers_CallsServiceForEach()
    {
        // Arrange
        var request = new SetRankTitleRequest("Elite");
        _mockUserService.Setup(s => s.SetUserRankTitleAsync(It.IsAny<int>(), "Elite")).ReturnsAsync(new UserRankTitleDto { UserId = 1 });

        // Act
        await _controller.SetUserTitle(1, request);
        await _controller.SetUserTitle(2, request);
        await _controller.SetUserTitle(3, request);

        // Assert
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(1, "Elite"), Times.Once);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(2, "Elite"), Times.Once);
        _mockUserService.Verify(s => s.SetUserRankTitleAsync(3, "Elite"), Times.Once);
    }
}




