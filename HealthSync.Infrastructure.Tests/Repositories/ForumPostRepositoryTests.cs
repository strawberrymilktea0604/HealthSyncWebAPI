using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ForumPostRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ForumPostRepository _repository;

    public ForumPostRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ForumPostRepository(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var category = new ForumCategory
        {
            CategoryId = 1,
            Name = "General Discussion",
            Description = "General forum discussions",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var posts = new List<Post>
        {
            new Post
            {
                PostId = 1,
                CategoryId = 1,
                UserId = 1,
                Title = "First Post",
                Content = "This is the first post content",
                ImageUrl = null,
                IsPinned = false,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Post
            {
                PostId = 2,
                CategoryId = 1,
                UserId = 1,
                Title = "Second Post",
                Content = "This is the second post content",
                ImageUrl = "https://example.com/image.jpg",
                IsPinned = true,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Post
            {
                PostId = 3,
                CategoryId = 1,
                UserId = 1,
                Title = "Third Post",
                Content = "This is the third post content",
                ImageUrl = null,
                IsPinned = false,
                IsLocked = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.ApplicationUsers.Add(user);
        _context.ForumCategories.Add(category);
        _context.Posts.AddRange(posts);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdWithRepliesAsync_ShouldReturnPostWithReplies_WhenPostExists()
    {
        // Act
        var result = await _repository.GetByIdWithRepliesAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PostId.Should().Be(1);
        result.Title.Should().Be("First Post");
        result.Content.Should().Be("This is the first post content");
        result.IsPinned.Should().BeFalse();
        result.IsLocked.Should().BeFalse();
        result.Category.Should().NotBeNull();
        result.Category.Name.Should().Be("General Discussion");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
        result.Replies.Should().NotBeNull();
        // Note: Replies collection will be empty since we didn't seed replies
        result.Replies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdWithRepliesAsync_ShouldReturnNull_WhenPostDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdWithRepliesAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPost_WhenPostExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PostId.Should().Be(1);
        result.Title.Should().Be("First Post");
        result.Content.Should().Be("This is the first post content");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPostDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenPostExists()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenPostDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewPostAndReturnIt()
    {
        // Arrange
        var newPost = new Post
        {
            CategoryId = 1,
            UserId = 1,
            Title = "New Post",
            Content = "New post content",
            ImageUrl = "https://example.com/new-image.jpg",
            IsPinned = false,
            IsLocked = false
        };

        // Act
        var result = await _repository.AddAsync(newPost);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Post");
        result.Content.Should().Be("New post content");
        result.ImageUrl.Should().Be("https://example.com/new-image.jpg");
        result.IsPinned.Should().BeFalse();
        result.IsLocked.Should().BeFalse();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var savedPost = await _context.Posts.FindAsync(result.PostId);
        savedPost.Should().NotBeNull();
        savedPost!.Title.Should().Be("New Post");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingPost()
    {
        // Arrange
        var post = await _repository.GetByIdAsync(1);
        post!.Title = "Updated Title";
        post.Content = "Updated content";
        post.IsPinned = true;
        post.ImageUrl = "https://example.com/updated.jpg";
        var originalUpdatedAt = post.UpdatedAt;

        // Act
        await _repository.UpdateAsync(post);

        // Assert
        var updatedPost = await _repository.GetByIdAsync(1);
        updatedPost.Should().NotBeNull();
        updatedPost!.Title.Should().Be("Updated Title");
        updatedPost.Content.Should().Be("Updated content");
        updatedPost.IsPinned.Should().BeTrue();
        updatedPost.ImageUrl.Should().Be("https://example.com/updated.jpg");
        updatedPost.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteExistingPost()
    {
        // Act
        await _repository.DeleteAsync(1);

        // Assert
        var deletedPost = await _repository.GetByIdAsync(1);
        deletedPost.Should().BeNull();

        var allPosts = await _repository.GetAllPostsAsync();
        allPosts.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenPostDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteAsync(999);

        var allPosts = await _repository.GetAllPostsAsync();
        allPosts.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllPostsAsync_ShouldReturnAllPosts()
    {
        // Act
        var result = await _repository.GetAllPostsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var posts = result.ToList();
        posts[0].PostId.Should().Be(1);
        posts[0].Title.Should().Be("First Post");
        posts[1].PostId.Should().Be(2);
        posts[1].Title.Should().Be("Second Post");
        posts[2].PostId.Should().Be(3);
        posts[2].Title.Should().Be("Third Post");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveChangesAndReturnAffectedRows()
    {
        // Arrange
        var post = new Post
        {
            CategoryId = 1,
            UserId = 1,
            Title = "Test Post",
            Content = "Test content"
        };
        _context.Posts.Add(post);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        post.PostId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(1, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        // Assert
        result.Should().Be(3); // All test posts are in current month
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnZero_WhenNoPostsInMonth()
    {
        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(1, 2020, 1);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnZero_WhenUserHasNoPosts()
    {
        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(999, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        // Assert
        result.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

