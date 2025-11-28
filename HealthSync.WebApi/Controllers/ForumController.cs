using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Forum;
using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/forum")]
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

    /// <summary>
    /// Create a new post in a forum category
    /// </summary>
    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var categoryExists = await _db.ForumCategories.AnyAsync(c => c.CategoryId == request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { success = false, message = "Invalid category" });
            }

            var post = new Post
            {
                CategoryId = request.CategoryId,
                UserId = userId,
                Title = request.Title,
                Content = request.Content,
                IsPinned = false,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            // TODO: Trigger background job to update user points (+2 for post)

            return CreatedAtAction(nameof(GetPostDetails), new { postId = post.PostId },
                new { success = true, data = new { postId = post.PostId }, message = "Post created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a reply to a post
    /// </summary>
    [HttpPost("posts/{postId}/replies")]
    public async Task<IActionResult> CreateReply(int postId, [FromBody] CreateReplyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            if (post.IsLocked)
            {
                return BadRequest(new { success = false, message = "Post is locked" });
            }

            var reply = new Reply
            {
                PostId = postId,
                UserId = userId,
                Content = request.Content,
                IsHidden = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Replies.Add(reply);
            await _db.SaveChangesAsync();

            // TODO: Trigger background job to update user points (+1 for reply)

            return CreatedAtAction(nameof(GetPostDetails), new { postId = postId },
                new { success = true, data = new { replyId = reply.ReplyId }, message = "Reply created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Update a post (only by the author)
    /// </summary>
    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdatePostRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            if (post.UserId != userId)
            {
                return Forbid();
            }

            post.Title = request.Title ?? post.Title;
            post.Content = request.Content ?? post.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Post updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a post (only by the author)
    /// </summary>
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            if (post.UserId != userId)
            {
                return Forbid();
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Post deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Update a reply (only by the author)
    /// </summary>
    [HttpPut("posts/{postId}/replies/{replyId}")]
    public async Task<IActionResult> UpdateReply(int postId, int replyId, [FromBody] UpdateReplyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var reply = await _db.Replies.FindAsync(replyId);
            if (reply == null || reply.PostId != postId)
            {
                return NotFound(new { success = false, message = "Reply not found" });
            }

            if (reply.UserId != userId)
            {
                return Forbid();
            }

            reply.Content = request.Content ?? reply.Content;
            reply.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Reply updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a reply (only by the author)
    /// </summary>
    [HttpDelete("posts/{postId}/replies/{replyId}")]
    public async Task<IActionResult> DeleteReply(int postId, int replyId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var reply = await _db.Replies.FindAsync(replyId);
            if (reply == null || reply.PostId != postId)
            {
                return NotFound(new { success = false, message = "Reply not found" });
            }

            if (reply.UserId != userId)
            {
                return Forbid();
            }

            _db.Replies.Remove(reply);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Reply deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Search posts by keyword
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string query,
        [FromQuery] int? categoryId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { success = false, message = "Search query is required" });
            }

            if (pageNumber < 1)
            {
                return BadRequest(new { success = false, message = "Page number must be >= 1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { success = false, message = "Page size must be between 1 and 100" });
            }

            var searchQuery = _db.Posts
                .Where(p => (p.Title.Contains(query) || p.Content.Contains(query)) &&
                           (!categoryId.HasValue || p.CategoryId == categoryId.Value))
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            var totalItems = await searchQuery.CountAsync();

            var items = await searchQuery
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

            return Ok(new { success = true, data = result, message = "Search completed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}