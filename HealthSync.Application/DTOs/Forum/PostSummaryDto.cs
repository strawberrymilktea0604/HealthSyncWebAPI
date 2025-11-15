using System;

namespace HealthSync.Application.DTOs.Forum;

public class PostSummaryDto
{
    public int PostId { get; set; }
    public int CategoryId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public int ReplyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}