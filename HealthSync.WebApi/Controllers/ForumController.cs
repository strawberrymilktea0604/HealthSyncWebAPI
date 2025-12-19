using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Forum;
using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;
using HealthSync.Application.Interfaces;
using System.Security.Claims;
using Hangfire;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/forum")]
[Authorize(Roles = "Customer")]
public class ForumController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storageService;
    private readonly IForumPostRepository _postRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ForumController(ApplicationDbContext db, IFileStorageService storageService, IForumPostRepository postRepository, IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _storageService = storageService;
        _postRepository = postRepository;
        _backgroundJobClient = backgroundJobClient;
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
    /// Create a new post in a forum category (supports optional image upload)
    /// </summary>
    [HttpPost("posts")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostWithImageRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { success = false, message = "Title and Content are required" });
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

            string? imageUrl = null;
            // Upload image if provided
            if (request.Image != null && request.Image.Length > 0)
            {
                // Validate image file
                var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedMimeTypes.Contains(request.Image.ContentType))
                {
                    return BadRequest(new { success = false, message = "Invalid image format. Allowed: JPEG, PNG, GIF, WebP" });
                }

                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (request.Image.Length > maxFileSize)
                {
                    return BadRequest(new { success = false, message = "Image size must not exceed 5MB" });
                }

                try
                {
                    imageUrl = await _storageService.UploadAsync(request.Image, "forum-posts");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Failed to upload image", error = ex.Message });
                }
            }

            var post = new Post
            {
                CategoryId = request.CategoryId,
                UserId = userId,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                ImageUrl = imageUrl,
                IsPinned = false,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _postRepository.AddAsync(post);

            // Trigger background job to update user points (+2 for post)
            _backgroundJobClient.Enqueue<ILeaderboardUpdateJob>(job => job.UpdateUserContributionPointsAsync(userId));

            return CreatedAtAction(nameof(GetPostDetails), new { postId = post.PostId },
                new { success = true, data = new { postId = post.PostId, imageUrl = imageUrl }, message = "Post created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new reply to a post
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

            // Trigger background job to update user points (+1 for reply)
            _backgroundJobClient.Enqueue<ILeaderboardUpdateJob>(job => job.UpdateUserContributionPointsAsync(userId));

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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePost(int postId, [FromForm] UpdatePostRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            // Verify ownership: only author can update their post
            if (post.UserId != userId)
            {
                return Forbid();
            }

            // Validate at least one field is being updated
            if (!IsUpdateRequestValid(request))
            {
                return BadRequest(new { success = false, message = "At least one field (title, content, or image) must be provided" });
            }

            // Update fields
            UpdatePostFields(post, request);

            // Handle image update
            if (request.Image != null && request.Image.Length > 0)
            {
                var imageResult = await HandleImageUpdateAsync(request.Image);
                if (imageResult is not OkResult)
                {
                    return imageResult;
                }
                post.ImageUrl = ((OkObjectResult)imageResult).Value as string;
            }

            // Update the UpdatedAt timestamp
            post.UpdatedAt = DateTime.UtcNow;

            // Persist changes via repository pattern
            await _postRepository.UpdateAsync(post);

            return Ok(new { 
                success = true, 
                data = new { 
                    postId = post.PostId, 
                    title = post.Title, 
                    content = post.Content, 
                    imageUrl = post.ImageUrl 
                }, 
                message = "Post updated successfully" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    private bool IsUpdateRequestValid(UpdatePostRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Title) || 
               !string.IsNullOrWhiteSpace(request.Content) || 
               request.Image != null;
    }

    private void UpdatePostFields(Post post, UpdatePostRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            post.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            post.Content = request.Content.Trim();
        }
    }

    private async Task<IActionResult> HandleImageUpdateAsync(IFormFile image)
    {
        // Validate image: MIME type whitelist (JPEG, PNG, GIF, WebP)
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(image.ContentType))
        {
            return BadRequest(new { success = false, message = "Invalid image format. Allowed: JPEG, PNG, GIF, WebP" });
        }

        // Validate size: max 5MB
        const long maxFileSize = 5 * 1024 * 1024;
        if (image.Length > maxFileSize)
        {
            return BadRequest(new { success = false, message = "Image size must not exceed 5MB" });
        }

        try
        {
            var newImageUrl = await _storageService.UploadAsync(image, "forum-posts");
            return Ok(newImageUrl);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to upload image", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a post (only by the author or Admin)
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

            var isAdmin = User.IsInRole("Admin");

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound(new { success = false, message = "Post not found" });
            }

            // Check authorization: owner or admin
            if (post.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            // Check if post has replies - cannot delete if has replies
            var hasReplies = await _db.Replies.AnyAsync(r => r.PostId == postId);
            if (hasReplies)
            {
                return BadRequest(new { success = false, message = "Cannot delete post that has replies" });
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