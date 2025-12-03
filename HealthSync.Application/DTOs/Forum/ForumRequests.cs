using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.DTOs.Forum;

public class CreatePostRequest
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CreatePostWithImageRequest
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public class CreateReplyRequest
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(2000, ErrorMessage = "Content must not exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;
}

public class UpdatePostRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public IFormFile? Image { get; set; }
}

public class UpdateReplyRequest
{
    public string? Content { get; set; }
}