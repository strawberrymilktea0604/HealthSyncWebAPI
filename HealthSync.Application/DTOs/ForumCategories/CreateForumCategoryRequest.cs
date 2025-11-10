using System;

namespace HealthSync.Application.DTOs.Forum;

public class CreateForumCategoryRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>
    /// Optional display order for sorting categories. Leave null to use default order logic.
    /// </summary>
    public int? DisplayOrder { get; set; }
}