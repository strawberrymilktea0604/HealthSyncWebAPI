using FluentAssertions;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Moq;
using Xunit;

namespace HealthSync.Application.Tests.Services;

public class ForumAdminServiceTests
{
    private readonly Mock<IForumPostRepository> _forumPostRepositoryMock;
    private readonly Mock<IForumReplyRepository> _forumReplyRepositoryMock;
    private readonly ForumAdminService _service;

    public ForumAdminServiceTests()
    {
        _forumPostRepositoryMock = new Mock<IForumPostRepository>();
        _forumReplyRepositoryMock = new Mock<IForumReplyRepository>();

        _service = new ForumAdminService(
            _forumPostRepositoryMock.Object,
            _forumReplyRepositoryMock.Object);
    }

    [Fact]
    public async Task DeletePostAsync_ExistingPost_ReturnsSuccess()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        _forumPostRepositoryMock.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.DeletePostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post deleted successfully");
    }

    [Fact]
    public async Task DeletePostAsync_NonExistingPost_ReturnsFailure()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await _service.DeletePostAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Post not found");
    }

    [Fact]
    public async Task PinPostAsync_ExistingPost_ReturnsSuccess()
    {
        // Arrange
        var post = new Post
        {
            PostId = 1,
            Title = "Test Post",
            Content = "Test content",
            IsPinned = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(post);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.PinPostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post pinned successfully");
        post.IsPinned.Should().BeTrue();
        post.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task PinPostAsync_NonExistingPost_ReturnsFailure()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Post?)null);

        // Act
        var result = await _service.PinPostAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Post not found");
    }

    [Fact]
    public async Task TogglePinPostAsync_UnpinnedPost_ReturnsSuccessAndPinned()
    {
        // Arrange
        var post = new Post
        {
            PostId = 1,
            Title = "Test Post",
            Content = "Test content",
            IsPinned = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(post);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.TogglePinPostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post pinned successfully");
        result.IsPinned.Should().BeTrue();
        post.IsPinned.Should().BeTrue();
        post.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task TogglePinPostAsync_PinnedPost_ReturnsSuccessAndUnpinned()
    {
        // Arrange
        var post = new Post
        {
            PostId = 1,
            Title = "Test Post",
            Content = "Test content",
            IsPinned = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(post);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.TogglePinPostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post unpinned successfully");
        result.IsPinned.Should().BeFalse();
        post.IsPinned.Should().BeFalse();
        post.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task TogglePinPostAsync_NonExistingPost_ReturnsFailure()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Post?)null);

        // Act
        var result = await _service.TogglePinPostAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Post not found");
        result.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task LockPostAsync_ExistingPost_ReturnsSuccess()
    {
        // Arrange
        var post = new Post
        {
            PostId = 1,
            Title = "Test Post",
            Content = "Test content",
            IsLocked = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(post);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.LockPostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post locked successfully");
        post.IsLocked.Should().BeTrue();
        post.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task LockPostAsync_NonExistingPost_ReturnsFailure()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Post?)null);

        // Act
        var result = await _service.LockPostAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Post not found");
    }

    [Fact]
    public async Task UnlockPostAsync_ExistingPost_ReturnsSuccess()
    {
        // Arrange
        var post = new Post
        {
            PostId = 1,
            Title = "Test Post",
            Content = "Test content",
            IsLocked = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(post);
        _forumPostRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.UnlockPostAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Post unlocked successfully");
        post.IsLocked.Should().BeFalse();
        post.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UnlockPostAsync_NonExistingPost_ReturnsFailure()
    {
        // Arrange
        _forumPostRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Post?)null);

        // Act
        var result = await _service.UnlockPostAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Post not found");
    }

    [Fact]
    public async Task HideReplyAsync_ExistingReply_ReturnsSuccess()
    {
        // Arrange
        var reply = new Reply
        {
            ReplyId = 1,
            PostId = 1,
            UserId = 1,
            Content = "Test reply",
            IsHidden = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _forumReplyRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(reply);
        _forumReplyRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.HideReplyAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Reply hidden successfully");
        reply.IsHidden.Should().BeTrue();
        reply.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task HideReplyAsync_NonExistingReply_ReturnsFailure()
    {
        // Arrange
        _forumReplyRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Reply?)null);

        // Act
        var result = await _service.HideReplyAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Reply not found");
    }

    [Fact]
    public async Task DeleteReplyAsync_ExistingReply_ReturnsSuccess()
    {
        // Arrange
        _forumReplyRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        _forumReplyRepositoryMock.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);
        _forumReplyRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteReplyAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Reply deleted successfully");
    }

    [Fact]
    public async Task DeleteReplyAsync_NonExistingReply_ReturnsFailure()
    {
        // Arrange
        _forumReplyRepositoryMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteReplyAsync(1, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Reply not found");
    }
}