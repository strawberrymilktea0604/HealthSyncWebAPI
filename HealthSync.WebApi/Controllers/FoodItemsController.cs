using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/food-items")]
public class FoodItemsController : ControllerBase
{
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly ILogger<FoodItemsController> _logger;

    public FoodItemsController(IFoodItemRepository foodItemRepository, ILogger<FoodItemsController> logger)
    {
        _foodItemRepository = foodItemRepository;
        _logger = logger;
    }

    /// <summary>
    /// Search food items with optional filters (public endpoint for customers)
    /// </summary>
    /// <param name="q">Search term to filter by name or description</param>
    /// <param name="category">Optional food category filter</param>
    /// <param name="page">Page number (starts at 1)</param>
    /// <param name="size">Page size (1-100)</param>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Page number must be greater than or equal to 1"
                });
            }

            if (size < 1 || size > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Page size must be between 1 and 100"
                });
            }

            // Validate and sanitize search term
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                if (q.Length > 200)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Search term must not exceed 200 characters"
                    });
                }
            }

            var result = await _foodItemRepository.SearchAsync(q, category, page, size);

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
            var foodItem = await _foodItemRepository.GetByIdAsync(id);
            
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

            // Check duplicate name
            if (await _foodItemRepository.ExistsByNameAsync(request.Name))
            {
                return BadRequest(new { success = false, message = "A food item with the same name already exists" });
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var entity = new FoodItem
            {
                Name = request.Name,
                Category = request.Category.ToString(),
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                ServingSize = request.ServingSize,
                ServingUnit = request.ServingUnit,
                CaloriesPerServing = request.CaloriesPerServing,
                ProteinG = request.ProteinG,
                CarbsG = request.CarbsG,
                FatG = request.FatG,
                FiberG = request.FiberG,
                SugarG = request.SugarG,
                CreatedByAdminId = adminId
            };

            var created = await _foodItemRepository.AddAsync(entity);

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

            var existingDto = await _foodItemRepository.GetByIdAsync(id);
            if (existingDto == null)
                return NotFound(new { success = false, message = "Food item not found" });

            if (await _foodItemRepository.ExistsByNameAsync(request.Name, id))
            {
                return BadRequest(new { success = false, message = "Another food item with the same name already exists" });
            }

            var efEntity = await _foodItemRepository.GetEntityByIdAsync(id);
            if (efEntity == null)
                return NotFound(new { success = false, message = "Food item not found" });

            // apply updates
            efEntity.Name = request.Name;
            efEntity.Category = request.Category.ToString();
            efEntity.Description = request.Description;
            efEntity.ImageUrl = request.ImageUrl;
            efEntity.ServingSize = request.ServingSize;
            efEntity.ServingUnit = request.ServingUnit;
            efEntity.CaloriesPerServing = request.CaloriesPerServing;
            efEntity.ProteinG = request.ProteinG;
            efEntity.CarbsG = request.CarbsG;
            efEntity.FatG = request.FatG;
            efEntity.FiberG = request.FiberG;
            efEntity.SugarG = request.SugarG;

            await _foodItemRepository.UpdateAsync(efEntity);

            return Ok(new { success = true, message = "Food item updated" });
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
            var existing = await _foodItemRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Food item not found" });

            if (await _foodItemRepository.IsUsedInFoodEntriesAsync(id))
            {
                return Conflict(new { success = false, message = "Food item is used in food entries and cannot be deleted" });
            }

            await _foodItemRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting food item with id {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
        }
    }
}
