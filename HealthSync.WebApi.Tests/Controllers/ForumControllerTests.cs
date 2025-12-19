using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using HealthSync.Application.DTOs.Forum;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class ForumControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<IForumPostRepository> _mockPostRepository;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly ForumController _controller;

    public ForumControllerTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        // Seed test data
        SeedTestData();

        // Setup mocks
        _mockStorageService = new Mock<IFileStorageService>();
        _mockPostRepository = new Mock<IForumPostRepository>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();

        // Create controller
        _controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);

        // Setup user claims for authenticated user
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

    private void SeedTestData()
    {
        // Seed Forum Category
        var category = new ForumCategory
        {
            CategoryId = 1,
            Name = "Test Category",
            Description = "Test Description",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ForumCategories.Add(category);

        // Seed User
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ApplicationUsers.Add(user);

        // Seed Post
        var post = new Post
        {
            PostId = 1,
            CategoryId = 1,
            UserId = 1,
            Title = "Test Post",
            Content = "Test Content",
            IsPinned = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(post);

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_ShouldReturnAllCategories_WhenCategoriesExist()
    {
        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        Assert.True((bool?)success == true);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange - Remove all categories
        _db.ForumCategories.RemoveRange(_db.ForumCategories);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    #endregion

    #region CreatePost Tests

    [Fact]
    public async Task CreatePost_ShouldReturnCreated_WhenValidRequestWithoutImage()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "New Post",
            Content = "New Content"
        };

        _mockPostRepository.Setup(x => x.AddAsync(It.IsAny<Post>()))
            .ReturnsAsync((Post p) => p);

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        // Verify background job was triggered - using Create() instead of Enqueue()
        // Note: Enqueue<T>() is an extension method and cannot be mocked directly
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob) && job.Method.Name == "UpdateUserContributionPointsAsync"),
            It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "",
            Content = "Content"
        };

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        // Verify background job was NOT triggered
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob)),
            It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnBadRequest_WhenContentIsEmpty()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "Title",
            Content = ""
        };

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnBadRequest_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 999,
            Title = "Title",
            Content = "Content"
        };

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region CreateReply Tests

    [Fact]
    public async Task CreateReply_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var request = new CreateReplyRequest
        {
            Content = "Test Reply"
        };

        // Act
        var result = await _controller.CreateReply(1, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        // Verify reply was added to database
        var reply = await _db.Replies.FirstOrDefaultAsync(r => r.PostId == 1);
        reply.Should().NotBeNull();
        reply!.Content.Should().Be("Test Reply");

        // Verify background job was triggered
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob) && job.Method.Name == "UpdateUserContributionPointsAsync"),
            It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var request = new CreateReplyRequest
        {
            Content = "Test Reply"
        };

        // Act
        var result = await _controller.CreateReply(999, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);

        // Verify background job was NOT triggered
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob)),
            It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnBadRequest_WhenPostIsLocked()
    {
        // Arrange
        var lockedPost = await _db.Posts.FindAsync(1);
        lockedPost!.IsLocked = true;
        await _db.SaveChangesAsync();

        var request = new CreateReplyRequest
        {
            Content = "Test Reply"
        };

        // Act
        var result = await _controller.CreateReply(1, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        // Verify background job was NOT triggered
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob)),
            It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnBadRequest_WhenContentIsEmpty()
    {
        // Arrange
        var request = new CreateReplyRequest
        {
            Content = ""
        };

        _controller.ModelState.AddModelError("Content", "Content is required");

        // Act
        var result = await _controller.CreateReply(1, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetPostsByCategory Tests

    [Fact]
    public async Task GetPostsByCategory_ShouldReturnPaginatedPosts_WhenValidRequest()
    {
        // Act
        var result = await _controller.GetPostsByCategory(1, 1, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _controller.GetPostsByCategory(999, 1, 20);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturnBadRequest_WhenPageNumberIsInvalid()
    {
        // Act
        var result = await _controller.GetPostsByCategory(1, 0, 20);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Act
        var result = await _controller.GetPostsByCategory(1, 1, 0);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreatePostAndReply_ShouldTriggerBackgroundJobTwice()
    {
        // Arrange - Create Post
        var postRequest = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "Integration Test Post",
            Content = "Integration Content"
        };

        _mockPostRepository.Setup(x => x.AddAsync(It.IsAny<Post>()))
            .ReturnsAsync((Post p) => p);

        // Act - Create Post
        var postResult = await _controller.CreatePost(postRequest);

        // Assert Post
        postResult.Should().BeOfType<CreatedAtActionResult>();

        // Verify first background job call (for post)
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob) && job.Method.Name == "UpdateUserContributionPointsAsync"),
            It.IsAny<IState>()),
            Times.Once);

        // Arrange - Create Reply
        var replyRequest = new CreateReplyRequest
        {
            Content = "Integration Reply"
        };

        // Act - Create Reply
        var replyResult = await _controller.CreateReply(1, replyRequest);

        // Assert Reply
        replyResult.Should().BeOfType<CreatedAtActionResult>();

        // Verify second background job call (for reply) - total 2 calls
        _mockBackgroundJobClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(ILeaderboardUpdateJob) && job.Method.Name == "UpdateUserContributionPointsAsync"),
            It.IsAny<IState>()),
            Times.Exactly(2));
    }

    #endregion
}
