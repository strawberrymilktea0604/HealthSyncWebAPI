using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/nutrition")]
[Authorize(Roles = "Customer")]
public class NutritionLogsController : ControllerBase
{
    private const string UserIdNotFoundMessage = "User ID not found in token";
    private readonly INutritionLogService _nutritionLogService;
    private readonly ILogger<NutritionLogsController> _logger;

    public NutritionLogsController(
        INutritionLogService nutritionLogService,
        ILogger<NutritionLogsController> logger)
    {
        _nutritionLogService = nutritionLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get or create nutrition log for a specific date
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <returns>Nutrition log for the date</returns>
    [HttpGet("daily/{date}")]
    [ProducesResponseType(typeof(NutritionLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionLogResponse>> GetDailyNutritionLog(string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out var logDate))
            {
                return BadRequest(new { success = false, message = "Invalid date format. Use YYYY-MM-DD." });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var result = await _nutritionLogService.GetOrCreateDailyLogAsync(userId, logDate);

            return Ok(new { success = true, data = result, message = "Nutrition log retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily nutrition log for date {Date}", date);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving nutrition log", error = ex.Message });
        }
    }

    /// <summary>
    /// Add a food entry to nutrition log for a specific date
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <param name="request">Food entry request</param>
    /// <returns>Updated nutrition log with new entry</returns>
    [HttpPost("daily/{date}/entries")]
    [ProducesResponseType(typeof(NutritionLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionLogResponse>> AddFoodEntry(string date, [FromBody] CreateFoodEntryRequest request)
    {
        try
        {
            if (!DateTime.TryParse(date, out var logDate))
            {
                return BadRequest(new { success = false, message = "Invalid date format. Use YYYY-MM-DD." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var result = await _nutritionLogService.AddFoodEntryAsync(userId, logDate, request);

            return Ok(new { success = true, data = result, message = "Food entry added successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for adding food entry");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding food entry for date {Date}", date);
            return StatusCode(500, new { success = false, message = "An error occurred while adding food entry", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a food entry
    /// </summary>
    /// <param name="id">Food entry ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("entries/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteFoodEntry(int id)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var deleted = await _nutritionLogService.DeleteFoodEntryAsync(userId, id);

            if (!deleted)
            {
                return NotFound(new { success = false, message = $"Food entry with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting food entry with ID {FoodEntryId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting food entry", error = ex.Message });
        }
    }

    /// <summary>
    /// Get nutrition logs history for the current user with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of nutrition logs</returns>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(PaginatedResult<NutritionLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetNutritionLogs(
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

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var result = await _nutritionLogService.GetNutritionLogsAsync(userId, pageNumber, pageSize);

            return Ok(new { success = true, data = result, message = "Nutrition logs retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nutrition logs");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving nutrition logs", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific nutrition log by ID
    /// </summary>
    /// <param name="id">Nutrition log ID</param>
    /// <returns>Nutrition log details</returns>
    [HttpGet("logs/{id}")]
    [ProducesResponseType(typeof(NutritionLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionLogResponse>> GetNutritionLogById(int id)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var result = await _nutritionLogService.GetByIdAsync(userId, id);

            if (result == null)
            {
                return NotFound(new { success = false, message = $"Nutrition log with ID {id} not found" });
            }

            return Ok(new { success = true, data = result, message = "Nutrition log retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nutrition log with ID {NutritionLogId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving nutrition log", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new nutrition log or add food entries to an existing log for a specific date
    /// </summary>
    /// <param name="request">Nutrition log creation request</param>
    /// <returns>Created or updated nutrition log with calculated totals</returns>
    [HttpPost("logs")]
    [ProducesResponseType(typeof(NutritionLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionLogResponse>> CreateNutritionLog([FromBody] CreateNutritionLogRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var response = await _nutritionLogService.CreateNutritionLogAsync(userId, request);

            return CreatedAtAction(
                nameof(GetNutritionLogById),
                new { id = response.NutritionLogId },
                new { success = true, data = response, message = "Nutrition log created successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for nutrition log creation");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating nutrition log");
            return StatusCode(500, new { success = false, message = "An error occurred while creating nutrition log", error = ex.Message });
        }
    }

    /// <summary>
    /// Update notes for a nutrition log
    /// </summary>
    /// <param name="id">Nutrition log ID</param>
    /// <param name="request">Update notes request</param>
    /// <returns>Updated nutrition log</returns>
    [HttpPut("logs/{id}/notes")]
    [ProducesResponseType(typeof(NutritionLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NutritionLogResponse>> UpdateNotes(
        int id,
        [FromBody] UpdateNutritionLogNotesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var response = await _nutritionLogService.UpdateNotesAsync(userId, id, request.Notes);

            return Ok(new { success = true, data = response, message = "Nutrition log notes updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Nutrition log with ID {NutritionLogId} not found", id);
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating nutrition log notes with ID {NutritionLogId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating nutrition log notes", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a nutrition log
    /// </summary>
    /// <param name="id">Nutrition log ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("logs/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteNutritionLog(int id)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
            }

            var deleted = await _nutritionLogService.DeleteAsync(userId, id);

            if (!deleted)
            {
                return NotFound(new { success = false, message = $"Nutrition log with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting nutrition log with ID {NutritionLogId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting nutrition log", error = ex.Message });
        }
    }
}
