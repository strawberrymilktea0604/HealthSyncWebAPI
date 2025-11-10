using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/admin/forum-categories")]
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _forumCategoryService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById), 
                new { id = result.Id }, 
                new { success = true, data = result, message = "Forum category created successfully" }
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating forum category");
            return StatusCode(500, new { success = false, message = "An error occurred while creating forum category" });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var categories = await _forumCategoryService.GetAllAsync();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forum categories");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving forum categories" });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var category = await _forumCategoryService.GetByIdAsync(id);
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateForumCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _forumCategoryService.UpdateAsync(id, request);

            return Ok(new { success = true, data = result, message = "Forum category updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Forum category not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forum category {Id}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating forum category" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _forumCategoryService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { success = false, message = "Forum category not found" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting forum category {Id}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting forum category" });
        }
    }
}