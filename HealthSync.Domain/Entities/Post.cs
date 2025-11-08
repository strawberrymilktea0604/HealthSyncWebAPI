namespace HealthSync.Domain.Entities;

public class Post
{
    public int PostId { get; set; }
    public int CategoryId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;
    public bool IsLocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ForumCategory Category { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Reply> Replies { get; set; } = new List<Reply>();
}