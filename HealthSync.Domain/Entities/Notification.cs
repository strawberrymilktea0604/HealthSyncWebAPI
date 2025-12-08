namespace HealthSync.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RecipientRole { get; set; } = string.Empty; // e.g. "Admin"
    public int? RecipientUserId { get; set; } = null; // optional, if targeting specific user
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? RelatedEntityId { get; set; } // optional, e.g. participationId
    public string? RelatedEntityType { get; set; }
}
