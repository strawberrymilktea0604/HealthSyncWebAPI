using System;

namespace HealthSync.Application.DTOs.Forum;

public class UpdateForumCategoryRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>
    /// Optional display order for sorting categories.
    /// </summary>
    public int? DisplayOrder { get; set; }
}