using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Forum;
using HealthSync.Application.DTOs;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/forum")]
[Authorize(Roles = "Customer")]
public class ForumController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ForumController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all forum categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _db.ForumCategories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    PostCount = c.Posts.Count()
                })
                .ToListAsync();

            return Ok(new { success = true, data = categories, message = "Categories retrieved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get posts in a specific forum category with pagination (customer)
    /// </summary>
    [HttpGet("categories/{categoryId}/posts")]
    public async Task<IActionResult> GetPostsByCategory(
        int categoryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageNumber < 1)
            {
                return BadRequest(new { success = false, message = "Page number must be >= 1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { success = false, message = "Page size must be between 1 and 100" });
            }

            var categoryExists = await _db.ForumCategories.AnyAsync(c => c.CategoryId == categoryId);
            if (!categoryExists)
            {
                return NotFound(new { success = false, message = "Forum category not found" });
            }

            var query = _db.Posts
                .Where(p => p.CategoryId == categoryId)
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .AsQueryable();

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostSummaryDto
                {
                    PostId = p.PostId,
                    CategoryId = p.CategoryId,
                    UserId = p.UserId,
                    Title = p.Title,
                    Excerpt = p.Content.Length > 200 ? p.Content.Substring(0, 200) : p.Content,
                    IsPinned = p.IsPinned,
                    IsLocked = p.IsLocked,
                    ReplyCount = p.Replies.Count(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            var result = new PaginatedResult<PostSummaryDto>(items, totalItems, pageNumber, pageSize);

            return Ok(new { success = true, data = result, message = "Posts retrieved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get post details with replies
    /// </summary>
    [HttpGet("posts/{postId}")]
    public async Task<IActionResult> GetPostDetails(int postId)
    {
        try
        {
            var post = await _db.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(p => p.Replies.Where(r => !r.IsHidden))
                    .ThenInclude(r => r.User)
                        .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            var postDetail = new PostDetailDto
            {
                PostId = post.PostId,
                CategoryId = post.CategoryId,
                CategoryName = post.Category.Name,
                UserId = post.UserId,
                UserName = post.User.UserProfile?.FullName ?? post.User.Email ?? "Unknown",
                Title = post.Title,
                Content = post.Content,
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                ReplyCount = post.Replies.Count(r => !r.IsHidden),
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Replies = post.Replies
                    .Where(r => !r.IsHidden)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new ReplyDto
                    {
                        ReplyId = r.ReplyId,
                        PostId = r.PostId,
                        UserId = r.UserId,
                        UserName = r.User.UserProfile?.FullName ?? r.User.Email ?? "Unknown",
                        Content = r.Content,
                        IsHidden = r.IsHidden,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToList()
            };

            return Ok(new { success = true, data = postDetail, message = "Post details retrieved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}