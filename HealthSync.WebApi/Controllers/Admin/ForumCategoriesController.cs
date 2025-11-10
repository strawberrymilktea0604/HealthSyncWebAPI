using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ForumCategoriesController : ControllerBase
{
    private readonly IForumCategoryService _forumCategoryService;
    private readonly ILogger<ForumCategoriesController> _logger;

    public ForumCategoriesController(
        IForumCategoryService forumCategoryService,
        ILogger<ForumCategoriesController> logger)
    {
        _forumCategoryService = forumCategoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all forum categories ordered by display order (Admin only)
    /// </summary>
    /// <returns>List of forum categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ForumCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ForumCategoryDto>>> GetAllCategories()
    {
        try
        {
            var categories = await _forumCategoryService.GetAllOrderedByDisplayOrderAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum categories");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get forum category by ID (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Forum category details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ForumCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForumCategoryDto>> GetCategoryById(int id)
    {
        try
        {
            var category = await _forumCategoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Forum category with ID {id} not found" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum category with ID {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get forum category by ID with posts count (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Forum category details with posts</returns>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(ForumCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForumCategoryDto>> GetCategoryWithPostsCount(int id)
    {
        try
        {
            var category = await _forumCategoryService.GetByIdWithPostsCountAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Forum category with ID {id} not found" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum category details with ID {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new forum category (Admin only)
    /// </summary>
    /// <param name="request">Category creation request</param>
    /// <returns>Created forum category</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ForumCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForumCategoryDto>> CreateCategory([FromBody] CreateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _forumCategoryService.CreateAsync(request);
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.Id },
                category);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create forum category: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating forum category");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing forum category (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Category update request</param>
    /// <returns>Updated forum category</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ForumCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForumCategoryDto>> UpdateCategory(
        int id,
        [FromBody] UpdateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _forumCategoryService.UpdateAsync(id, request);
            return Ok(category);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Forum category with ID {CategoryId} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update forum category: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forum category with ID {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete forum category (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var deleted = await _forumCategoryService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Forum category with ID {id} not found" });
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete forum category with ID {CategoryId}: {Message}", id, ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting forum category with ID {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if a forum category exists by ID (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Boolean indicating if category exists</returns>
    [HttpGet("{id}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> CategoryExists(int id)
    {
        try
        {
            var exists = await _forumCategoryService.ExistsAsync(id);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if forum category with ID {CategoryId} exists", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if a category has related posts (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Boolean indicating if category has posts</returns>
    [HttpGet("{id}/has-posts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> HasRelatedPosts(int id)
    {
        try
        {
            var hasPosts = await _forumCategoryService.HasRelatedPostsAsync(id);
            return Ok(new { hasPosts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if forum category with ID {CategoryId} has posts", id);
            return StatusCode(500, "Internal server error");
        }
    }
}