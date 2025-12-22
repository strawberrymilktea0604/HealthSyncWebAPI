using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.WebApi.Controllers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers.Admin;

public class UsersControllerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _loggerMock = new Mock<ILogger<UsersController>>();

        _controller = new UsersController(
            _userRepositoryMock.Object,
            _userProfileRepositoryMock.Object,
            _leaderboardRepositoryMock.Object
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
    public async Task GetUsers_ReturnsSuccess_WhenUsersExist()
    {
        // Arrange
        var page = 1;
        var size = 20;
        var search = "test";
        var role = "Customer";

        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                UserId = 1,
                Email = "user1@test.com",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        var paginatedResult = new PaginatedResult<ApplicationUser>
        {
            Items = users,
            CurrentPage = page,
            PageSize = size,
            TotalItems = 1,
            TotalPages = 1
        };

        var userProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = 1,
            FullName = "Test User"
        };

        _userRepositoryMock
            .Setup(x => x.GetUsersAsync(page, size, search, role))
            .ReturnsAsync(paginatedResult);

        _userProfileRepositoryMock
            .Setup(x => x.GetByUserIdAsync(1))
            .ReturnsAsync(userProfile);

        // Act
        var result = await _controller.GetUsers(page, size, search, role);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var itemsProperty = responseType.GetProperty("Items");
        var currentPageProperty = responseType.GetProperty("CurrentPage");
        var pageSizeProperty = responseType.GetProperty("PageSize");
        var totalItemsProperty = responseType.GetProperty("TotalItems");
        var totalPagesProperty = responseType.GetProperty("TotalPages");

        Assert.NotNull(itemsProperty);
        Assert.NotNull(currentPageProperty);
        Assert.NotNull(pageSizeProperty);
        Assert.NotNull(totalItemsProperty);
        Assert.NotNull(totalPagesProperty);

        var itemsValue = itemsProperty.GetValue(response);
        var currentPageValue = currentPageProperty.GetValue(response);
        var pageSizeValue = pageSizeProperty.GetValue(response);
        var totalItemsValue = totalItemsProperty.GetValue(response);
        var totalPagesValue = totalPagesProperty.GetValue(response);

        Assert.NotNull(itemsValue);
        Assert.NotNull(currentPageValue);
        Assert.NotNull(pageSizeValue);
        Assert.NotNull(totalItemsValue);
        Assert.NotNull(totalPagesValue);

        Assert.IsType<List<AdminUserDto>>(itemsValue);
        var itemsList = (List<AdminUserDto>)itemsValue;
        Assert.Single(itemsList);
        Assert.Equal(1, itemsList[0].Id);
        Assert.Equal("user1@test.com", itemsList[0].Email);
        Assert.Equal("Customer", itemsList[0].Role);
        Assert.True(itemsList[0].IsActive);
        Assert.Equal("Test User", itemsList[0].FullName);

        Assert.Equal(page, currentPageValue);
        Assert.Equal(size, pageSizeValue);
        Assert.Equal(1, totalItemsValue);
        Assert.Equal(1, totalPagesValue);
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccess_WhenNoUsersExist()
    {
        // Arrange
        var page = 1;
        var size = 20;

        var paginatedResult = new PaginatedResult<ApplicationUser>
        {
            Items = new List<ApplicationUser>(),
            CurrentPage = page,
            PageSize = size,
            TotalItems = 0,
            TotalPages = 0
        };

        _userRepositoryMock
            .Setup(x => x.GetUsersAsync(page, size, null, null))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetUsers(page, size, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var itemsProperty = responseType.GetProperty("Items");
        var totalItemsProperty = responseType.GetProperty("TotalItems");

        Assert.NotNull(itemsProperty);
        Assert.NotNull(totalItemsProperty);

        var itemsValue = itemsProperty.GetValue(response);
        var totalItemsValue = totalItemsProperty.GetValue(response);

        Assert.NotNull(itemsValue);
        Assert.NotNull(totalItemsValue);

        Assert.IsType<List<AdminUserDto>>(itemsValue);
        var itemsList = (List<AdminUserDto>)itemsValue;
        Assert.Empty(itemsList);
        Assert.Equal(0, totalItemsValue);
    }

    [Fact]
    public async Task GetUsers_ValidatesPageParameters()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<ApplicationUser>
        {
            Items = new List<ApplicationUser>(),
            CurrentPage = 1,
            PageSize = 20,
            TotalItems = 0,
            TotalPages = 0
        };

        _userRepositoryMock
            .Setup(x => x.GetUsersAsync(1, 20, null, null))
            .ReturnsAsync(paginatedResult);

        // Act - Invalid page and size should be corrected to 1 and 20
        var result = await _controller.GetUsers(0, 0, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _userRepositoryMock.Verify(x => x.GetUsersAsync(1, 20, null, null), Times.Once);
    }

    [Fact]
    public async Task GetUserDetails_ReturnsSuccess_WhenUserExists()
    {
        // Arrange
        var userId = 1;

        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "user@test.com",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = userId,
            FullName = "Test User",
            Gender = HealthSync.Domain.Entities.Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 175,
            CurrentWeightKg = 70,
            ActivityLevel = HealthSync.Domain.Entities.ActivityLevel.ModeratelyActive,
            AvatarUrl = "avatar.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var leaderboard = new Leaderboard
        {
            LeaderboardId = 1,
            UserId = userId,
            TotalPoints = 150,
            UpdatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);
        _leaderboardRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(leaderboard);
        _userRepositoryMock.Setup(x => x.GetTotalWorkoutsAsync(userId)).ReturnsAsync(10);
        _userRepositoryMock.Setup(x => x.GetTotalNutritionLogsAsync(userId)).ReturnsAsync(5);
        _userRepositoryMock.Setup(x => x.GetTotalGoalsAsync(userId)).ReturnsAsync(3);
        _userRepositoryMock.Setup(x => x.GetTotalChallengesAsync(userId)).ReturnsAsync(2);

        // Act
        var result = await _controller.GetUserDetails(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");

        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);

        var successValue = successProperty.GetValue(response);
        var dataValue = dataProperty.GetValue(response);

        Assert.NotNull(successValue);
        Assert.NotNull(dataValue);

        Assert.True((bool)successValue);

        var dataType = dataValue.GetType();
        var userProperty = dataType.GetProperty("User");
        var profileProperty = dataType.GetProperty("Profile");
        var statsProperty = dataType.GetProperty("Stats");

        Assert.NotNull(userProperty);
        Assert.NotNull(profileProperty);
        Assert.NotNull(statsProperty);

        var userValue = userProperty.GetValue(dataValue);
        var profileValue = profileProperty.GetValue(dataValue);
        var statsValue = statsProperty.GetValue(dataValue);

        Assert.NotNull(userValue);
        Assert.NotNull(profileValue);
        Assert.NotNull(statsValue);

        // Verify User DTO
        var userDtoType = userValue.GetType();
        Assert.Equal(userId, userDtoType.GetProperty("Id")?.GetValue(userValue));
        Assert.Equal("user@test.com", userDtoType.GetProperty("Email")?.GetValue(userValue));
        Assert.Equal("Customer", userDtoType.GetProperty("Role")?.GetValue(userValue));

        var isActiveValue = userDtoType.GetProperty("IsActive")?.GetValue(userValue);
        Assert.NotNull(isActiveValue);
        Assert.True((bool)isActiveValue);

        Assert.Equal("Test User", userDtoType.GetProperty("FullName")?.GetValue(userValue));

        // Verify Profile DTO
        var profileDtoType = profileValue.GetType();
        Assert.Equal(userId, profileDtoType.GetProperty("UserId")?.GetValue(profileValue));
        Assert.Equal("Test User", profileDtoType.GetProperty("FullName")?.GetValue(profileValue));
        Assert.Equal("Male", profileDtoType.GetProperty("Gender")?.GetValue(profileValue));
        Assert.Equal(175m, profileDtoType.GetProperty("HeightCm")?.GetValue(profileValue));
        Assert.Equal(70m, profileDtoType.GetProperty("CurrentWeightKg")?.GetValue(profileValue));
        Assert.Equal("avatar.jpg", profileDtoType.GetProperty("AvatarUrl")?.GetValue(profileValue));
        Assert.Equal(150, profileDtoType.GetProperty("ContributionPoints")?.GetValue(profileValue));

        // Verify Stats DTO
        var statsDtoType = statsValue.GetType();
        Assert.Equal(10, statsDtoType.GetProperty("TotalWorkouts")?.GetValue(statsValue));
        Assert.Equal(5, statsDtoType.GetProperty("TotalNutritionLogs")?.GetValue(statsValue));
        Assert.Equal(3, statsDtoType.GetProperty("TotalGoals")?.GetValue(statsValue));
        Assert.Equal(2, statsDtoType.GetProperty("TotalChallenges")?.GetValue(statsValue));
        Assert.Equal(150, statsDtoType.GetProperty("ContributionPoints")?.GetValue(statsValue));
    }

    [Fact]
    public async Task GetUserDetails_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 1;
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetUserDetails(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
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
    public async Task GetUserDetails_ReturnsNotFound_WhenUserProfileDoesNotExist()
    {
        // Arrange
        var userId = 1;

        var user = new ApplicationUser
        {
            UserId = userId,
            Email = "user@test.com",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _controller.GetUserDetails(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);

        var responseType = response.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);

        var messageValue = messageProperty.GetValue(response);
        Assert.NotNull(messageValue);
        Assert.Equal("User profile not found", messageValue);
    }
}

