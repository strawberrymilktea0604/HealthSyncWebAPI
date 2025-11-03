using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ExercisesController : ControllerBase
{
    private readonly IExerciseService _exerciseService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(IExerciseService exerciseService, ILogger<ExercisesController> logger)
    {
        _exerciseService = exerciseService;
        _logger = logger;
    }

    /// <summary>
    /// Get exercises with filters and pagination (Admin only)
    /// </summary>
    /// <param name="muscleGroup">Filter by muscle group</param>
    /// <param name="difficulty">Filter by difficulty level</param>
    /// <param name="equipment">Filter by equipment</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of exercises</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ExerciseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ExerciseDto>>> GetExercises(
        [FromQuery] string? muscleGroup,
        [FromQuery] string? difficulty,
        [FromQuery] string? equipment,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (pageNumber < 1) pageNumber = 1;

            var result = await _exerciseService.GetExercisesAsync(muscleGroup, difficulty, equipment, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercises with filters");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get exercise by ID (Admin only)
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <returns>Exercise details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> GetExerciseById(int id)
    {
        try
        {
            var exercise = await _exerciseService.GetExerciseByIdAsync(id);
            if (exercise == null)
            {
                return NotFound($"Exercise with ID {id} not found");
            }
            return Ok(exercise);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercise with ID {ExerciseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new exercise (Admin only)
    /// </summary>
    /// <param name="request">Exercise creation request</param>
    /// <returns>Created exercise</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExerciseDto>> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var exercise = await _exerciseService.CreateExerciseAsync(request, adminId);
            return CreatedAtAction(nameof(GetExerciseById), new { id = exercise.Id }, exercise);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exercise");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing exercise (Admin only)
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="request">Exercise update request</param>
    /// <returns>Updated exercise</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(int id, [FromBody] UpdateExerciseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exercise = await _exerciseService.UpdateExerciseAsync(id, request);
            return Ok(exercise);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exercise with ID {ExerciseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload image for exercise (Admin only)
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="file">Image file</param>
    /// <returns>Updated exercise with image URL</returns>
    [HttpPost("{id}/image")]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> UploadExerciseImage(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file type and size
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only JPG and PNG files are allowed");
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB
            {
                return BadRequest("File size must be less than 5MB");
            }

            var exercise = await _exerciseService.UploadExerciseImageAsync(id, file);
            return Ok(exercise);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for exercise with ID {ExerciseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete exercise (Admin only)
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        try
        {
            await _exerciseService.DeleteExerciseAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exercise with ID {ExerciseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
