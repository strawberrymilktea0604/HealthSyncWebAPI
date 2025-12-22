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
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections;
using System.Security.Claims;
using FluentAssertions;

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

        // Seed UserProfile
        var userProfile = new UserProfile
        {
            UserProfileId = 1,
            UserId = 1,
            FullName = "Test User",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            HeightCm = 175,
            CurrentWeightKg = 70,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            AvatarUrl = null,
            ContributionPoints = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.UserProfiles.Add(userProfile);

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

        // Seed another post without replies for delete testing
        var post2 = new Post
        {
            PostId = 2,
            CategoryId = 1,
            UserId = 2, // Different user
            Title = "Test Post 2",
            Content = "Test Content 2",
            IsPinned = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(post2);

        // Seed another user for post 2
        var user2 = new ApplicationUser
        {
            UserId = 2,
            Email = "test2@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ApplicationUsers.Add(user2);

        // Seed UserProfile for user 2
        var userProfile2 = new UserProfile
        {
            UserProfileId = 2,
            UserId = 2,
            FullName = "Test User 2",
            Gender = Gender.Female,
            DateOfBirth = new DateTime(1992, 1, 1),
            HeightCm = 165,
            CurrentWeightKg = 60,
            ActivityLevel = ActivityLevel.LightlyActive,
            AvatarUrl = null,
            ContributionPoints = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.UserProfiles.Add(userProfile2);

        // Seed Replies
        var reply1 = new Reply
        {
            ReplyId = 1,
            PostId = 1,
            UserId = 1,
            Content = "Test Reply 1",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Replies.Add(reply1);

        var reply2 = new Reply
        {
            ReplyId = 2,
            PostId = 1,
            UserId = 1,
            Content = "Test Reply 2",
            IsHidden = true, // Hidden reply
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Replies.Add(reply2);

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
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
        Assert.True(success is true);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange - Remove all data to avoid cascade delete issues
        _db.Replies.RemoveRange(_db.Replies);
        _db.Posts.RemoveRange(_db.Posts);
        _db.UserProfiles.RemoveRange(_db.UserProfiles);
        _db.ApplicationUsers.RemoveRange(_db.ApplicationUsers);
        _db.ForumCategories.RemoveRange(_db.ForumCategories);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    #endregion

    #region GetPostDetails Tests

    [Fact]
    public async Task GetPostDetails_ShouldReturnPostDetails_WhenPostExists()
    {
        // Act
        var result = await _controller.GetPostDetails(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Post details retrieved successfully", message);

        Assert.NotNull(data);
        var postId = data.GetType().GetProperty("PostId")?.GetValue(data);
        var title = data.GetType().GetProperty("Title")?.GetValue(data);
        var content = data.GetType().GetProperty("Content")?.GetValue(data);
        var categoryName = data.GetType().GetProperty("CategoryName")?.GetValue(data);
        var userName = data.GetType().GetProperty("UserName")?.GetValue(data);
        var isPinned = data.GetType().GetProperty("IsPinned")?.GetValue(data);
        var isLocked = data.GetType().GetProperty("IsLocked")?.GetValue(data);
        var replyCount = data.GetType().GetProperty("ReplyCount")?.GetValue(data);
        var replies = data.GetType().GetProperty("Replies")?.GetValue(data) as IList;

        Assert.Equal(1, postId);
        Assert.Equal("Test Post", title);
        Assert.Equal("Test Content", content);
        Assert.Equal("Test Category", categoryName);
        Assert.Equal("Test User", userName);
        Assert.False(isPinned is true);
        Assert.False(isLocked is true);
        Assert.Equal(1, replyCount);
        Assert.NotNull(replies);
        Assert.Single(replies);
    }

    [Fact]
    public async Task GetPostDetails_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Act
        var result = await _controller.GetPostDetails(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Post not found", message);
    }

    [Fact]
    public async Task GetPostDetails_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange - Create a new controller with disposed context to simulate exception
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedDb = new ApplicationDbContext(options);
        disposedDb.Dispose(); // Dispose the context to cause exception

        var controller = new ForumController(disposedDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.GetPostDetails(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.NotNull(error);
        if (error != null)
        {
            Assert.False(string.IsNullOrEmpty(error.ToString()));
        }
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
        var replies = await _db.Replies.Where(r => r.PostId == 1).ToListAsync();
        var newReply = replies.OrderByDescending(r => r.ReplyId).First(); // Get the latest reply
        newReply.Content.Should().Be("Test Reply");

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

    #region UpdatePost Tests

    [Fact]
    public async Task UpdatePost_ShouldUpdatePostSuccessfully_WhenValidRequestAndOwner()
    {
        // Arrange
        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title",
            Content = "Updated content for the post"
        };

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Post updated successfully", message);

        // Verify data contains updated fields
        Assert.NotNull(data);
        var postId = data.GetType().GetProperty("postId")?.GetValue(data);
        var title = data.GetType().GetProperty("title")?.GetValue(data);
        var content = data.GetType().GetProperty("content")?.GetValue(data);

        Assert.Equal(1, postId);
        Assert.Equal("Updated Title", title);
        Assert.Equal("Updated content for the post", content);

        // Verify repository was called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.Is<Post>(p =>
            p.PostId == 1 &&
            p.Title == "Updated Title" &&
            p.Content == "Updated content for the post")), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_ShouldUpdatePostWithImageSuccessfully_WhenValidImage()
    {
        // Arrange
        var mockImage = new Mock<IFormFile>();
        mockImage.Setup(f => f.ContentType).Returns("image/jpeg");
        mockImage.Setup(f => f.Length).Returns(1024 * 1024); // 1MB

        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title with Image",
            Image = mockImage.Object
        };

        _mockStorageService.Setup(x => x.UploadAsync(It.IsAny<IFormFile>(), "forum-posts"))
            .ReturnsAsync("https://storage.example.com/forum-posts/new-image.jpg");

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var data = value.GetType().GetProperty("data")?.GetValue(value);

        Assert.True((bool?)success);

        // Verify data contains image URL
        Assert.NotNull(data);
        var imageUrl = data.GetType().GetProperty("imageUrl")?.GetValue(data);
        Assert.Equal("https://storage.example.com/forum-posts/new-image.jpg", imageUrl);

        // Verify storage service was called
        _mockStorageService.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), "forum-posts"), Times.Once);

        // Verify repository was called with updated image URL
        _mockPostRepository.Verify(x => x.UpdateAsync(It.Is<Post>(p =>
            p.PostId == 1 &&
            p.Title == "Updated Title with Image" &&
            p.ImageUrl == "https://storage.example.com/forum-posts/new-image.jpg")), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title"
        };

        // Act
        var result = await _controller.UpdatePost(999, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Post not found", message);

        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnForbid_WhenUserIsNotOwner()
    {
        // Arrange - Use existing post owned by different user (post 2 is owned by user 2)
        var updateRequest = new UpdatePostRequest
        {
            Title = "Trying to update other user's post"
        };

        // Act - User 1 tries to update post 2 (owned by user 2)
        var result = await _controller.UpdatePost(2, updateRequest);

        // Assert
        result.Should().BeOfType<ForbidResult>();

        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnBadRequest_WhenNoFieldsProvided()
    {
        // Arrange
        var updateRequest = new UpdatePostRequest
        {
            Title = "",
            Content = null,
            Image = null
        };

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("At least one field (title, content, or image) must be provided", message);

        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnBadRequest_WhenImageFormatInvalid()
    {
        // Arrange
        var mockImage = new Mock<IFormFile>();
        mockImage.Setup(f => f.ContentType).Returns("text/plain"); // Invalid format
        mockImage.Setup(f => f.Length).Returns(1024);

        var updateRequest = new UpdatePostRequest
        {
            Image = mockImage.Object
        };

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid image format. Allowed: JPEG, PNG, GIF, WebP", message);

        // Verify storage service was not called
        _mockStorageService.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnBadRequest_WhenImageTooLarge()
    {
        // Arrange
        var mockImage = new Mock<IFormFile>();
        mockImage.Setup(f => f.ContentType).Returns("image/jpeg");
        mockImage.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10MB - too large

        var updateRequest = new UpdatePostRequest
        {
            Image = mockImage.Object
        };

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Image size must not exceed 5MB", message);

        // Verify storage service was not called
        _mockStorageService.Verify(x => x.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Create controller without user claims
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No User set
        };

        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title"
        };

        // Act
        var result = await controller.UpdatePost(1, updateRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();

        var value = unauthorizedResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False((bool?)success);
        Assert.Equal("Invalid user", message);

        // Verify repository was not called
        _mockPostRepository.Verify(x => x.UpdateAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title"
        };

        // Setup repository to throw exception
        _mockPostRepository.Setup(x => x.UpdateAsync(It.IsAny<Post>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdatePost(1, updateRequest);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.NotNull(error);
        Assert.Equal("Database error", error);
    }

    #endregion

    #region DeletePost Tests

    [Fact]
    public async Task DeletePost_ShouldDeletePostSuccessfully_WhenOwnerAndNoReplies()
    {
        // Arrange - Post 2 belongs to user 2 and has no replies

        // Act - User 1 (current user) tries to delete post 2 (belongs to user 2) - should fail
        var result = await _controller.DeletePost(2);

        // Assert - Should return Forbid because user 1 is not the owner
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeletePost_ShouldDeletePostSuccessfully_WhenOwnerAndNoReplies_User2()
    {
        // Arrange - Switch to user 2 who owns post 2
        var user2Claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "2")
        };
        var user2Identity = new ClaimsIdentity(user2Claims);
        var user2Principal = new ClaimsPrincipal(user2Identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user2Principal }
        };

        // Setup mock to return completed task (repository is mocked)
        _mockPostRepository.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act - User 2 deletes their own post 2 (which has no replies)
        var result = await _controller.DeletePost(2);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Post deleted successfully", message);

        // Verify repository DeleteAsync was called with correct postId
        _mockPostRepository.Verify(r => r.DeleteAsync(2), Times.Once);
    }

    [Fact]
    public async Task DeletePost_ShouldDeletePostSuccessfully_WhenAdminAndNoReplies()
    {
        // Arrange - Set user as admin
        var adminClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var adminIdentity = new ClaimsIdentity(adminClaims);
        var adminPrincipal = new ClaimsPrincipal(adminIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminPrincipal }
        };

        // Setup mock to return completed task (repository is mocked)
        _mockPostRepository.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act - Admin deletes post 2 (which belongs to user 2 and has no replies)
        var result = await _controller.DeletePost(2);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Post deleted successfully", message);

        // Verify repository DeleteAsync was called with correct postId
        _mockPostRepository.Verify(r => r.DeleteAsync(2), Times.Once);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Act
        var result = await _controller.DeletePost(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Post not found", message);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnForbid_WhenUserIsNotOwner()
    {
        // Act - User 1 tries to delete post 2 (which belongs to user 2)
        var result = await _controller.DeletePost(2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeletePost_ShouldDeletePostSuccessfully_WhenAdminAndHasReplies()
    {
        // Arrange - Admin user
        var adminClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var adminIdentity = new ClaimsIdentity(adminClaims);
        var adminPrincipal = new ClaimsPrincipal(adminIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminPrincipal }
        };

        // Setup mock to return completed task (repository is mocked)
        _mockPostRepository.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act - Admin deletes post 1 (which has replies)
        var result = await _controller.DeletePost(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Post deleted successfully", message);

        // Verify repository DeleteAsync was called with correct postId
        _mockPostRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange - Set user as admin so authorization passes
        var adminClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var adminIdentity = new ClaimsIdentity(adminClaims);
        var adminPrincipal = new ClaimsPrincipal(adminIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminPrincipal }
        };

        // Setup repository to throw exception
        _mockPostRepository.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act - Try to delete post 2 as admin
        var result = await _controller.DeletePost(2);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.NotNull(error);
        Assert.Equal("Database error", error);
    }

    #endregion

    #region UpdateReply Tests

    [Fact]
    public async Task UpdateReply_ShouldUpdateReplySuccessfully_WhenOwner()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = "Updated reply content" };

        // Act
        var result = await _controller.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Reply updated successfully", message);

        // Verify reply was updated in database
        var updatedReply = await _db.Replies.FindAsync(1);
        Assert.NotNull(updatedReply);
        Assert.Equal("Updated reply content", updatedReply.Content);
        Assert.True(updatedReply.UpdatedAt > updatedReply.CreatedAt);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        // Add model state error
        _controller.ModelState.AddModelError("Content", "Content is required");

        var request = new UpdateReplyRequest { Content = "" };

        // Act
        var result = await _controller.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid input", message);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - Set unauthenticated user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // Empty claims principal
        };

        var request = new UpdateReplyRequest { Content = "Updated content" };

        // Act
        var result = await _controller.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult.StatusCode.Should().Be(401);

        var value = unauthorizedResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Invalid user", message);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnNotFound_WhenReplyDoesNotExist()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = "Updated content" };

        // Act - Try to update non-existent reply
        var result = await _controller.UpdateReply(1, 999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Reply not found", message);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnNotFound_WhenReplyPostIdMismatch()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = "Updated content" };

        // Act - Try to update reply 1 with wrong postId (should be postId=1)
        var result = await _controller.UpdateReply(999, 1, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);

        var value = notFoundResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("Reply not found", message);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnForbid_WhenUserIsNotOwner()
    {
        // Arrange - User 2 trying to update reply owned by user 1
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = "Updated content" };

        // Act - User 2 tries to update reply 1 (owned by user 1)
        var result = await _controller.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        var forbidResult = result as ForbidResult;
        forbidResult.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReply_ShouldUpdateReplySuccessfully_WhenContentIsNull()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = null };

        // Act
        var result = await _controller.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);

        Assert.True(success is true);
        Assert.Equal("Reply updated successfully", message);

        // Verify reply content was NOT changed (null content means keep existing)
        var updatedReply = await _db.Replies.FindAsync(1);
        Assert.NotNull(updatedReply);
        Assert.Equal("Test Reply 1", updatedReply.Content); // Original content from seed data
        Assert.True(updatedReply.UpdatedAt > updatedReply.CreatedAt);
    }

    [Fact]
    public async Task UpdateReply_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var userIdentity = new ClaimsIdentity(userClaims);
        var userPrincipal = new ClaimsPrincipal(userIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var request = new UpdateReplyRequest { Content = "Updated content" };

        // Create a controller with a disposed context to simulate database exception
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb_Exception")
            .Options;
        var exceptionDb = new ApplicationDbContext(options);
        exceptionDb.Dispose(); // Dispose the context to cause ObjectDisposedException

        var controllerWithException = new ForumController(
            exceptionDb,
            _mockStorageService.Object,
            _mockPostRepository.Object,
            _mockBackgroundJobClient.Object);

        controllerWithException.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        // Act
        var result = await controllerWithException.UpdateReply(1, 1, request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult.StatusCode.Should().Be(500);

        var value = statusCodeResult.Value;
        Assert.NotNull(value);
        var success = value.GetType().GetProperty("success")?.GetValue(value);
        var message = value.GetType().GetProperty("message")?.GetValue(value);
        var error = value.GetType().GetProperty("error")?.GetValue(value);

        Assert.False(success is true);
        Assert.Equal("An error occurred", message);
        Assert.Contains("Cannot access a disposed", error?.ToString());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetCategories_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // No claims
        };

        // Act
        var result = await controller.GetCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCategories_ShouldReturn500_WhenExceptionOccurs()
    {
        // Arrange - Create controller with disposed context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedDb = new ApplicationDbContext(options);
        disposedDb.Dispose();

        var controller = new ForumController(disposedDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.GetCategories();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await controller.GetPostsByCategory(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturn500_WhenExceptionOccurs()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedDb = new ApplicationDbContext(options);
        disposedDb.Dispose();

        var controller = new ForumController(disposedDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.GetPostsByCategory(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPostDetails_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await controller.GetPostDetails(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "Test",
            Content = "Test"
        };

        // Act
        var result = await controller.CreatePost(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task CreatePost_ShouldReturn500_WhenExceptionOccurs()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "Test",
            Content = "Test"
        };

        _mockPostRepository.Setup(x => x.AddAsync(It.IsAny<Post>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateReplyRequest
        {
            Content = "Test reply"
        };

        // Act
        var result = await controller.CreateReply(1, request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateReply_ShouldReturn500_WhenExceptionOccurs()
    {
        // Arrange
        var request = new CreateReplyRequest
        {
            Content = "Test reply"
        };

        // Mock the database to throw exception
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var testDb = new ApplicationDbContext(options);
        testDb.Dispose(); // Dispose to cause exception

        var controller = new ForumController(testDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.CreateReply(1, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnUnauthorized_WhenUserNotAuthenticated_Duplicate()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new UpdatePostRequest
        {
            Title = "Updated",
            Content = "Updated"
        };

        // Act
        var result = await controller.UpdatePost(1, request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var controller = new ForumController(_db, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await controller.DeletePost(1);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region Additional Error Handling Tests

    [Fact]
    public async Task GetCategories_ShouldReturn500_WhenExceptionOccurs_Duplicate()
    {
        // Arrange - Create controller with disposed context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedDb = new ApplicationDbContext(options);
        disposedDb.Dispose();

        var controller = new ForumController(disposedDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.GetCategories();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPostsByCategory_ShouldReturn500_WhenExceptionOccurs_Duplicate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedDb = new ApplicationDbContext(options);
        disposedDb.Dispose();

        var controller = new ForumController(disposedDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.GetPostsByCategory(1);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreatePost_ShouldReturn500_WhenExceptionOccurs_Duplicate()
    {
        // Arrange
        var request = new CreatePostWithImageRequest
        {
            CategoryId = 1,
            Title = "Test",
            Content = "Test"
        };

        _mockPostRepository.Setup(x => x.AddAsync(It.IsAny<Post>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreatePost(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateReply_ShouldReturn500_WhenExceptionOccurs_Duplicate()
    {
        // Arrange
        var request = new CreateReplyRequest
        {
            Content = "Test reply"
        };

        // Mock the database to throw exception
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var testDb = new ApplicationDbContext(options);
        testDb.Dispose(); // Dispose to cause exception

        var controller = new ForumController(testDb, _mockStorageService.Object, _mockPostRepository.Object, _mockBackgroundJobClient.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Customer")
                }))
            }
        };

        // Act
        var result = await controller.CreateReply(1, request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion
}

