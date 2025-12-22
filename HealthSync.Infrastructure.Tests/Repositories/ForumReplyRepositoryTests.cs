using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class ForumReplyRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ForumReplyRepository _repository;

    public ForumReplyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ForumReplyRepository(_context);

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

        var post = new Post
        {
            PostId = 1,
            CategoryId = 1,
            UserId = 1,
            Title = "Test Post",
            Content = "This is a test post",
            IsPinned = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var replies = new List<Reply>
        {
            new Reply
            {
                ReplyId = 1,
                PostId = 1,
                UserId = 1,
                Content = "First reply to the post",
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Reply
            {
                ReplyId = 2,
                PostId = 1,
                UserId = 1,
                Content = "Second reply to the post",
                IsHidden = true,
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                UpdatedAt = DateTime.UtcNow.AddHours(-12)
            },
            new Reply
            {
                ReplyId = 3,
                PostId = 1,
                UserId = 1,
                Content = "Third reply to the post",
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                UpdatedAt = DateTime.UtcNow.AddHours(-6)
            }
        };

        _context.ApplicationUsers.Add(user);
        _context.ForumCategories.Add(category);
        _context.Posts.Add(post);
        _context.Replies.AddRange(replies);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReply_WhenReplyExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ReplyId.Should().Be(1);
        result.Content.Should().Be("First reply to the post");
        result.IsHidden.Should().BeFalse();
        result.Post.Should().NotBeNull();
        result.Post.Title.Should().Be("Test Post");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenReplyDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenReplyExists()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenReplyDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingReply()
    {
        // Arrange
        var reply = await _repository.GetByIdAsync(1);
        reply!.Content = "Updated reply content";
        reply.IsHidden = true;
        var originalUpdatedAt = reply.UpdatedAt;

        // Act
        await _repository.UpdateAsync(reply);

        // Assert
        var updatedReply = await _repository.GetByIdAsync(1);
        updatedReply.Should().NotBeNull();
        updatedReply!.Content.Should().Be("Updated reply content");
        updatedReply.IsHidden.Should().BeTrue();
        updatedReply.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteExistingReply()
    {
        // Act
        await _repository.DeleteAsync(1);

        // Assert
        var deletedReply = await _repository.GetByIdAsync(1);
        deletedReply.Should().BeNull();

        var allReplies = await _repository.GetAllAsync();
        allReplies.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenReplyDoesNotExist()
    {
        // Act & Assert
        await _repository.DeleteAsync(999);

        var allReplies = await _repository.GetAllAsync();
        allReplies.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllReplies()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var replies = result.ToList();
        replies[0].ReplyId.Should().Be(1);
        replies[0].Content.Should().Be("First reply to the post");
        replies[1].ReplyId.Should().Be(2);
        replies[1].Content.Should().Be("Second reply to the post");
        replies[2].ReplyId.Should().Be(3);
        replies[2].Content.Should().Be("Third reply to the post");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveChangesAndReturnAffectedRows()
    {
        // Arrange
        var reply = new Reply
        {
            PostId = 1,
            UserId = 1,
            Content = "New reply"
        };
        _context.Replies.Add(reply);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        reply.ReplyId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(1, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        // Assert
        result.Should().Be(3); // All test replies are in current month
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnZero_WhenNoRepliesInMonth()
    {
        // Act
        var result = await _repository.CountByUserIdAndMonthAsync(1, 2020, 1);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CountByUserIdAndMonthAsync_ShouldReturnZero_WhenUserHasNoReplies()
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

