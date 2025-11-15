using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/foods")]
public class FoodsController : ControllerBase
{
    private readonly IFoodItemService _foodItemService;
    private readonly ILogger<FoodsController> _logger;

    public FoodsController(IFoodItemService foodItemService, ILogger<FoodsController> logger)
    {
        _foodItemService = foodItemService;
        _logger = logger;
    }

    /// <summary>
    /// Search food items (Customer access)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] string? category)
    {
        try
        {
            // Validate and sanitize search term
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                if (search.Length > 200)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Search term must not exceed 200 characters"
                    });
                }
            }

            var result = await _foodItemService.SearchAsync(search, category, 1, 100); // Default pagination

            return Ok(new
            {
                success = true,
                data = result,
                message = "Food items retrieved successfully"
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while searching food items",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get a specific food item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var foodItem = await _foodItemService.GetByIdAsync(id);
            
            if (foodItem == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Food item not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = foodItem,
                message = "Food item retrieved successfully"
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving food item",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Create a new FoodItem (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateFoodItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _foodItemService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = created.FoodItemId }, new { success = true, data = created, message = "Food item created" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating food item");
            return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update existing FoodItem (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFoodItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _foodItemService.UpdateAsync(id, request);

            return Ok(new { success = true, data = updated, message = "Food item updated" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Food item not found" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating food item with id {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete food item (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _foodItemService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Food item not found or cannot be deleted" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting food item with id {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
        }
    }
}
