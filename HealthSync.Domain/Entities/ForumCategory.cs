namespace HealthSync.Domain.Entities;

public class ForumCategory
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public object? ForumCategoryId { get; set; }
}