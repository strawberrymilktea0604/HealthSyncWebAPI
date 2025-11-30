namespace HealthSync.Application.DTOs.Forum;

public class CreatePostRequest
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CreateReplyRequest
{
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class UpdatePostRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}

public class UpdateReplyRequest
{
    public string? Content { get; set; }
}