using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class NotificationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationRepository _repository;

    public NotificationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new NotificationRepository(_context);
    }

    [Fact]
    public async Task AddAsync_AddsNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test Notification",
            Message = "This is a test message",
            RecipientRole = "Admin",
            IsRead = false
        };

        // Act
        var result = await _repository.AddAsync(notification);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Notification", result.Title);
        Assert.Equal("This is a test message", result.Message);
        Assert.Equal("Admin", result.RecipientRole);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task GetByRoleAsync_ReturnsNotificationsForRole()
    {
        // Arrange
        var notification1 = new Notification { Title = "Admin Notification 1", Message = "Message 1", RecipientRole = "Admin", CreatedAt = DateTime.UtcNow };
        var notification2 = new Notification { Title = "Admin Notification 2", Message = "Message 2", RecipientRole = "Admin", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var notification3 = new Notification { Title = "Customer Notification", Message = "Message 3", RecipientRole = "Customer", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };

        _context.Notifications.AddRange(notification1, notification2, notification3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRoleAsync("Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, n => Assert.Equal("Admin", n.RecipientRole));
        Assert.True(result.First().CreatedAt >= result.Last().CreatedAt); // Ordered by CreatedAt descending
    }

    [Fact]
    public async Task GetByRoleAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            var notification = new Notification
            {
                Title = $"Notification {i}",
                Message = $"Message {i}",
                RecipientRole = "Admin",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _repository.GetByRoleAsync("Admin", page: 1, pageSize: 10);
        var page2 = await _repository.GetByRoleAsync("Admin", page: 2, pageSize: 10);
        var page3 = await _repository.GetByRoleAsync("Admin", page: 3, pageSize: 10);

        // Assert
        Assert.Equal(10, page1.Count());
        Assert.Equal(10, page2.Count());
        Assert.Equal(5, page3.Count());
    }

    [Fact]
    public async Task GetByRoleAsync_NoMatchingRole_ReturnsEmpty()
    {
        // Arrange
        var notification = new Notification { Title = "Test", Message = "Message", RecipientRole = "Admin", CreatedAt = DateTime.UtcNow };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRoleAsync("Customer");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test Notification",
            Message = "Test Message",
            RecipientRole = "Admin"
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(notification.NotificationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(notification.NotificationId, result.NotificationId);
        Assert.Equal("Test Notification", result.Title);
        Assert.Equal("Test Message", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAsRead()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Unread Notification",
            Message = "Message",
            RecipientRole = "Admin",
            IsRead = false
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        await _repository.MarkAsReadAsync(notification.NotificationId);
        await _context.SaveChangesAsync();

        // Assert
        var updatedNotification = await _context.Notifications.FindAsync(notification.NotificationId);
        Assert.NotNull(updatedNotification);
        Assert.True(updatedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistingId_DoesNotThrow()
    {
        // Act & Assert
        await _repository.MarkAsReadAsync(999);
        // Should not throw exception
    }

    [Fact]
    public async Task GetUnreadCountByRoleAsync_ReturnsCorrectCount()
    {
        // Arrange
        var notification1 = new Notification { Title = "N1", Message = "M1", RecipientRole = "Admin", IsRead = false };
        var notification2 = new Notification { Title = "N2", Message = "M2", RecipientRole = "Admin", IsRead = false };
        var notification3 = new Notification { Title = "N3", Message = "M3", RecipientRole = "Admin", IsRead = true };
        var notification4 = new Notification { Title = "N4", Message = "M4", RecipientRole = "Customer", IsRead = false };

        _context.Notifications.AddRange(notification1, notification2, notification3, notification4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnreadCountByRoleAsync("Admin");

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetUnreadCountByRoleAsync_NoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        var notification = new Notification { Title = "Test", Message = "Message", RecipientRole = "Admin", IsRead = true };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnreadCountByRoleAsync("Admin");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetUnreadCountByRoleAsync_NoMatchingRole_ReturnsZero()
    {
        // Arrange
        var notification = new Notification { Title = "Test", Message = "Message", RecipientRole = "Admin", IsRead = false };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnreadCountByRoleAsync("Customer");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsSavedChangesCount()
    {
        // Arrange
        var notification = new Notification { Title = "Test", Message = "Message", RecipientRole = "Admin" };
        _context.Notifications.Add(notification);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public async Task AddAsync_WithRecipientUserId_SavesCorrectly()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "User Specific Notification",
            Message = "This is for user 123",
            RecipientRole = "Customer",
            RecipientUserId = 123
        };

        // Act
        var result = await _repository.AddAsync(notification);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.RecipientUserId);
    }

    [Fact]
    public async Task AddAsync_WithRelatedEntity_SavesCorrectly()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Challenge Notification",
            Message = "Challenge has been approved",
            RecipientRole = "Customer",
            RelatedEntityId = 456,
            RelatedEntityType = "ChallengeParticipation"
        };

        // Act
        var result = await _repository.AddAsync(notification);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(456, result.RelatedEntityId);
        Assert.Equal("ChallengeParticipation", result.RelatedEntityType);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
