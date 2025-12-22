using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class NotificationTests
{
    [Fact]
    public void Notification_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var notification = new Notification();

        // Assert
        Assert.Equal(0, notification.NotificationId);
        Assert.Equal(string.Empty, notification.Title);
        Assert.Equal(string.Empty, notification.Message);
        Assert.Equal(string.Empty, notification.RecipientRole);
        Assert.Null(notification.RecipientUserId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.RelatedEntityId);
        Assert.Null(notification.RelatedEntityType);
    }

    [Fact]
    public void Notification_SetProperties_StoresValuesCorrectly()
    {
        // Arrange
        var notification = new Notification();
        var createdAt = DateTime.UtcNow;

        // Act
        notification.NotificationId = 1;
        notification.Title = "Test Notification";
        notification.Message = "This is a test message";
        notification.RecipientRole = "Admin";
        notification.RecipientUserId = 123;
        notification.IsRead = true;
        notification.CreatedAt = createdAt;
        notification.RelatedEntityId = 456;
        notification.RelatedEntityType = "Challenge";

        // Assert
        Assert.Equal(1, notification.NotificationId);
        Assert.Equal("Test Notification", notification.Title);
        Assert.Equal("This is a test message", notification.Message);
        Assert.Equal("Admin", notification.RecipientRole);
        Assert.Equal(123, notification.RecipientUserId);
        Assert.True(notification.IsRead);
        Assert.Equal(createdAt, notification.CreatedAt);
        Assert.Equal(456, notification.RelatedEntityId);
        Assert.Equal("Challenge", notification.RelatedEntityType);
    }

    [Fact]
    public void Notification_CreatedAt_DefaultsToUtcNow()
    {
        // Arrange & Act
        var notification = new Notification();
        var now = DateTime.UtcNow;

        // Assert
        Assert.True(notification.CreatedAt <= now);
        Assert.True(notification.CreatedAt >= now.AddSeconds(-1));
    }

    [Fact]
    public void Notification_IsRead_DefaultsToFalse()
    {
        // Arrange & Act
        var notification = new Notification();

        // Assert
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Notification_CanSetTitleToEmptyString()
    {
        // Arrange & Act
        var notification = new Notification { Title = "" };

        // Assert
        Assert.Equal("", notification.Title);
    }

    [Fact]
    public void Notification_CanSetMessageToEmptyString()
    {
        // Arrange & Act
        var notification = new Notification { Message = "" };

        // Assert
        Assert.Equal("", notification.Message);
    }

    [Fact]
    public void Notification_CanSetRecipientRoleToEmptyString()
    {
        // Arrange & Act
        var notification = new Notification { RecipientRole = "" };

        // Assert
        Assert.Equal("", notification.RecipientRole);
    }

    [Fact]
    public void Notification_RecipientUserId_CanBeNull()
    {
        // Arrange & Act
        var notification = new Notification { RecipientUserId = null };

        // Assert
        Assert.Null(notification.RecipientUserId);
    }

    [Fact]
    public void Notification_RelatedEntityId_CanBeNull()
    {
        // Arrange & Act
        var notification = new Notification { RelatedEntityId = null };

        // Assert
        Assert.Null(notification.RelatedEntityId);
    }

    [Fact]
    public void Notification_RelatedEntityType_CanBeNull()
    {
        // Arrange & Act
        var notification = new Notification { RelatedEntityType = null };

        // Assert
        Assert.Null(notification.RelatedEntityType);
    }

    [Fact]
    public void Notification_ForAdminRole_SetsCorrectly()
    {
        // Arrange & Act
        var notification = new Notification
        {
            Title = "Admin Notification",
            Message = "System alert",
            RecipientRole = "Admin"
        };

        // Assert
        Assert.Equal("Admin", notification.RecipientRole);
    }

    [Fact]
    public void Notification_ForCustomerRole_SetsCorrectly()
    {
        // Arrange & Act
        var notification = new Notification
        {
            Title = "Customer Notification",
            Message = "Challenge approved",
            RecipientRole = "Customer",
            RecipientUserId = 456
        };

        // Assert
        Assert.Equal("Customer", notification.RecipientRole);
        Assert.Equal(456, notification.RecipientUserId);
    }

    [Fact]
    public void Notification_WithRelatedEntity_StoresCorrectly()
    {
        // Arrange & Act
        var notification = new Notification
        {
            Title = "Challenge Update",
            Message = "Your challenge has been reviewed",
            RecipientRole = "Customer",
            RecipientUserId = 789,
            RelatedEntityId = 123,
            RelatedEntityType = "ChallengeParticipation"
        };

        // Assert
        Assert.Equal(123, notification.RelatedEntityId);
        Assert.Equal("ChallengeParticipation", notification.RelatedEntityType);
    }

    [Fact]
    public void Notification_MarkAsRead_ChangesIsReadToTrue()
    {
        // Arrange
        var notification = new Notification { IsRead = false };

        // Act
        notification.IsRead = true;

        // Assert
        Assert.True(notification.IsRead);
    }

    [Fact]
    public void Notification_CanStoreMultilineMessage()
    {
        // Arrange
        var multilineMessage = "Line 1\nLine 2\nLine 3";

        // Act
        var notification = new Notification { Message = multilineMessage };

        // Assert
        Assert.Equal(multilineMessage, notification.Message);
        Assert.Contains("\n", notification.Message);
    }

    [Fact]
    public void Notification_CanStoreLongMessage()
    {
        // Arrange
        var longMessage = new string('A', 1000);

        // Act
        var notification = new Notification { Message = longMessage };

        // Assert
        Assert.Equal(1000, notification.Message.Length);
    }

    [Fact]
    public void Notification_CanStoreUnicodeCharacters()
    {
        // Arrange & Act
        var notification = new Notification
        {
            Title = "通知 Notification",
            Message = "こんにちは Hello 😊"
        };

        // Assert
        Assert.Contains("通知", notification.Title);
        Assert.Contains("こんにちは", notification.Message);
        Assert.Contains("😊", notification.Message);
    }

    [Fact]
    public void Notification_CreatedAt_CanBeSetManually()
    {
        // Arrange
        var specificDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var notification = new Notification { CreatedAt = specificDate };

        // Assert
        Assert.Equal(specificDate, notification.CreatedAt);
    }

    [Fact]
    public void Notification_MultipleInstances_HaveIndependentValues()
    {
        // Arrange & Act
        var notification1 = new Notification { Title = "Notification 1", IsRead = true };
        var notification2 = new Notification { Title = "Notification 2", IsRead = false };

        // Assert
        Assert.NotEqual(notification1.Title, notification2.Title);
        Assert.NotEqual(notification1.IsRead, notification2.IsRead);
    }
}
