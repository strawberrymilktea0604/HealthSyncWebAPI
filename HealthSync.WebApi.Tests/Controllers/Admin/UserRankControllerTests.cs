using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class UserRankControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILeaderboardRepository> _mockLeaderboardRepository;
    private readonly UserRankController _controller;

    public UserRankControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLeaderboardRepository = new Mock<ILeaderboardRepository>();
        _controller = new UserRankController(_mockUserRepository.Object, _mockLeaderboardRepository.Object);
    }

    [Fact]
    public async Task SetUserRankTitle_UserExists_LeaderboardExists_ReturnsOk()
    {
        // Arrange
        var user = new ApplicationUser { UserId = 1, Email = "test@test.com", Role = "Customer" };
        var leaderboard = new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100 };
        var request = new SetRankTitleRequest("Top Contributor");

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(leaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRankTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => l.RankTitle == "Top Contributor")), Times.Once);
    }

    [Fact]
    public async Task SetUserRankTitle_UserExists_LeaderboardNotExists_CreatesLeaderboard()
    {
        // Arrange
        var user = new ApplicationUser { UserId = 1, Email = "test@test.com", Role = "Customer" };
        var request = new SetRankTitleRequest("Rising Star");

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((Leaderboard?)null);
        _mockLeaderboardRepository.Setup(r => r.AddAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRankTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLeaderboardRepository.Verify(r => r.AddAsync(It.Is<Leaderboard>(l => 
            l.UserId == 1 && 
            l.RankTitle == "Rising Star" && 
            l.TotalPoints == 0)), Times.Once);
    }

    [Fact]
    public async Task SetUserRankTitle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new SetRankTitleRequest("Top Contributor");
        _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.SetUserRankTitle(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task SetUserRankTitle_UpdatesTimestamp()
    {
        // Arrange
        var user = new ApplicationUser { UserId = 1, Email = "test@test.com", Role = "Customer" };
        var leaderboard = new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        var request = new SetRankTitleRequest("Elite Member");

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(leaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRankTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => 
            l.UpdatedAt > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [Fact]
    public async Task SetUserRankTitle_NullRankTitle_ReturnsOk()
    {
        // Arrange
        var user = new ApplicationUser { UserId = 1, Email = "test@test.com", Role = "Customer" };
        var leaderboard = new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100, RankTitle = "Old Title" };
        var request = new SetRankTitleRequest(null);

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(leaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRankTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => l.RankTitle == null)), Times.Once);
    }

    [Fact]
    public async Task SetUserRankTitle_EmptyRankTitle_ReturnsOk()
    {
        // Arrange
        var user = new ApplicationUser { UserId = 1, Email = "test@test.com", Role = "Customer" };
        var leaderboard = new Leaderboard { LeaderboardId = 1, UserId = 1, TotalPoints = 100 };
        var request = new SetRankTitleRequest("");

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockLeaderboardRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(leaderboard);
        _mockLeaderboardRepository.Setup(r => r.UpdateAsync(It.IsAny<Leaderboard>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetUserRankTitle(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockLeaderboardRepository.Verify(r => r.UpdateAsync(It.Is<Leaderboard>(l => l.RankTitle == "")), Times.Once);
    }
}


