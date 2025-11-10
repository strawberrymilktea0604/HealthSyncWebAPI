using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.FoodItems;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class FoodController : ControllerBase
{
    private readonly IFoodItemService _foodItemService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FoodController> _logger;

    public FoodController(IFoodItemService foodItemService, IFileStorageService fileStorageService, ILogger<FoodController> logger)
    {
        _foodItemService = foodItemService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all food items with pagination and optional search/filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm, 
        [FromQuery] string? category,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _foodItemService.SearchAsync(searchTerm, category, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving food items");
            return StatusCode(500, "An error occurred while retrieving food items");
        }
    }

    /// <summary>
    /// Get a specific food item by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var item = await _foodItemService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"Food item with ID {id} not found" });
            }
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving food item {FoodItemId}", id);
            return StatusCode(500, "An error occurred while retrieving the food item");
        }
    }

    /// <summary>
    /// Create a new food item
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateFoodItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _foodItemService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.FoodItemId }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating food item");
            return StatusCode(500, "An error occurred while creating the food item");
        }
    }

    /// <summary>
    /// Update an existing food item
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFoodItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _foodItemService.UpdateAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = $"Food item with ID {id} not found" });
            }
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating food item {FoodItemId}", id);
            return StatusCode(500, "An error occurred while updating the food item");
        }
    }

    /// <summary>
    /// Upload image for a food item
    /// </summary>
    [HttpPost("{id}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest("Invalid file type. Only JPEG and PNG are allowed.");
        }

        // Validate file size (5MB max)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 5MB limit.");
        }

        try
        {
            // Check if food item exists
            var foodItem = await _foodItemService.GetByIdAsync(id);
            if (foodItem == null)
            {
                return NotFound(new { message = $"Food item with ID {id} not found" });
            }

            // Upload to MinIO
            var imageUrl = await _fileStorageService.UploadFileAsync(file, "foods", id.ToString());

            // Update food item with image URL
            var updateRequest = new UpdateFoodItemRequest
            {
                Name = foodItem.Name,
                Category = foodItem.Category,
                ServingSize = foodItem.ServingSize,
                ServingUnit = foodItem.ServingUnit,
                CaloriesPerServing = foodItem.CaloriesPerServing,
                ProteinG = foodItem.ProteinG,
                CarbsG = foodItem.CarbsG,
                FatG = foodItem.FatG,
                FiberG = foodItem.FiberG,
                SugarG = foodItem.SugarG,
                Description = foodItem.Description,
                ImageUrl = imageUrl
            };

            var updated = await _foodItemService.UpdateAsync(id, updateRequest);
            return Ok(new { imageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for food item {FoodItemId}", id);
            return StatusCode(500, "An error occurred while uploading the image");
        }
    }

    /// <summary>
    /// Delete a food item
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _foodItemService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Food item with ID {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting food item {FoodItemId}", id);
            return StatusCode(500, "An error occurred while deleting the food item");
        }
    }
}
