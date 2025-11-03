using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
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
    /// Get all exercises (Admin only)
    /// </summary>
    /// <returns>List of all exercises</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExerciseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetAllExercises()
    {
        try
        {
            var exercises = await _exerciseService.GetAllExercisesAsync();
            return Ok(exercises);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercises");
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

            var exercise = await _exerciseService.CreateExerciseAsync(request);
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
