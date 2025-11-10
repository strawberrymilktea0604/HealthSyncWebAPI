using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/forum-categories")]
public class ForumCategoriesController : ControllerBase
{
    private readonly IForumCategoryRepository _forumCategoryRepository;
    private readonly ILogger<ForumCategoriesController> _logger;

    public ForumCategoriesController(
        IForumCategoryRepository forumCategoryRepository,
        ILogger<ForumCategoriesController> logger)
    {
        _forumCategoryRepository = forumCategoryRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check duplicate name
            if (await _forumCategoryRepository.ExistsByNameAsync(request.Name))
            {
                return BadRequest(new { success = false, message = "A category with this name already exists" });
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            var entity = new ForumCategory
            {
                Name = request.Name,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
#pragma warning restore CS8601 // Possible null reference assignment.

            var created = await _forumCategoryRepository.AddAsync(entity);

            return CreatedAtAction(
                nameof(GetById), 
                new { id = created.CategoryId }, 
                new { success = true, data = created, message = "Forum category created successfully" }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating forum category");
            return StatusCode(500, new { success = false, message = "An error occurred while creating forum category" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var categories = await _forumCategoryRepository.GetAllAsync();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum categories");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving forum categories" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var category = await _forumCategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { success = false, message = "Forum category not found" });
            }

            return Ok(new { success = true, data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum category {Id}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving forum category" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingCategory = await _forumCategoryRepository.GetByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound(new { success = false, message = "Forum category not found" });
            }

            // Check duplicate name but exclude current category
            if (await _forumCategoryRepository.ExistsByNameAsync(request.Name, id))
            {
                return BadRequest(new { success = false, message = "Another category with this name already exists" });
            }

            existingCategory.Name = request.Name;
#pragma warning disable CS8601 // Possible null reference assignment.
            existingCategory.Description = request.Description;
#pragma warning restore CS8601 // Possible null reference assignment.
            existingCategory.DisplayOrder = request.DisplayOrder ?? existingCategory.DisplayOrder;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await _forumCategoryRepository.UpdateAsync(existingCategory);

            return Ok(new { success = true, data = existingCategory, message = "Forum category updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forum category {Id}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating forum category" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await _forumCategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { success = false, message = "Forum category not found" });
            }

            // Check if category has related posts
            if (await _forumCategoryRepository.HasRelatedPostsAsync(id))
            {
                return Conflict(new { success = false, message = "Cannot delete category with existing posts" });
            }

            await _forumCategoryRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting forum category {Id}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting forum category" });
        }
    }
}