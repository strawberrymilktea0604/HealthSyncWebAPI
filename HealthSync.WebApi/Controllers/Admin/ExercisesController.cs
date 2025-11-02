using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ExercisesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(ApplicationDbContext context, ILogger<ExercisesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all exercises (Admin only)
    /// </summary>
    /// <returns>List of all exercises</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExerciseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExerciseResponse>>> GetAllExercises()
    {
        try
        {
            var exercises = await _context.Exercises
                .OrderBy(e => e.Name)
                .Select(e => new ExerciseResponse
                {
                    Id = e.ExerciseId,
                    Name = e.Name,
                    MuscleGroup = e.MuscleGroup,
                    Difficulty = e.Difficulty,
                    Equipment = e.Equipment,
                    Description = e.Description,
                    ImageUrl = e.ImageUrl,
                    CreatedAt = DateTime.UtcNow, // TODO: Add CreatedAt to Exercise entity
                    UpdatedAt = null // TODO: Add UpdatedAt to Exercise entity
                })
                .ToListAsync();

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
    [ProducesResponseType(typeof(ExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse>> GetExerciseById(int id)
    {
        try
        {
            var exercise = await _context.Exercises.FindAsync(id);

            if (exercise == null)
            {
                return NotFound($"Exercise with ID {id} not found");
            }

            var response = new ExerciseResponse
            {
                Id = exercise.ExerciseId,
                Name = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Difficulty = exercise.Difficulty,
                Equipment = exercise.Equipment,
                Description = exercise.Description,
                ImageUrl = exercise.ImageUrl,
                CreatedAt = DateTime.UtcNow, // TODO: Add CreatedAt to Exercise entity
                UpdatedAt = null // TODO: Add UpdatedAt to Exercise entity
            };

            return Ok(response);
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
    [ProducesResponseType(typeof(ExerciseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExerciseResponse>> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if exercise with same name already exists
            var existingExercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower());

            if (existingExercise != null)
            {
                return BadRequest("Exercise with this name already exists");
            }

            var exercise = new Exercise
            {
                Name = request.Name,
                MuscleGroup = request.MuscleGroup ?? "General",
                Difficulty = request.Difficulty ?? "Beginner",
                Equipment = request.Equipment,
                Description = request.Description,
                ImageUrl = null // TODO: Handle image upload if needed
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var response = new ExerciseResponse
            {
                Id = exercise.ExerciseId,
                Name = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Difficulty = exercise.Difficulty,
                Equipment = exercise.Equipment,
                Description = exercise.Description,
                ImageUrl = exercise.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            return CreatedAtAction(
                nameof(GetExerciseById),
                new { id = exercise.ExerciseId },
                response);
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
    [ProducesResponseType(typeof(ExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse>> UpdateExercise(int id, [FromBody] UpdateExerciseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
            {
                return NotFound($"Exercise with ID {id} not found");
            }

            // Check if another exercise with same name exists (excluding current exercise)
            var existingExercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower() && e.ExerciseId != id);

            if (existingExercise != null)
            {
                return BadRequest("Another exercise with this name already exists");
            }

            // Update properties
            exercise.Name = request.Name;
            exercise.MuscleGroup = request.MuscleGroup ?? exercise.MuscleGroup;
            exercise.Difficulty = request.Difficulty ?? exercise.Difficulty;
            exercise.Equipment = request.Equipment;
            exercise.Description = request.Description;

            await _context.SaveChangesAsync();

            var response = new ExerciseResponse
            {
                Id = exercise.ExerciseId,
                Name = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Difficulty = exercise.Difficulty,
                Equipment = exercise.Equipment,
                Description = exercise.Description,
                ImageUrl = exercise.ImageUrl,
                CreatedAt = DateTime.UtcNow, // TODO: Add CreatedAt to Exercise entity
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(response);
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
            var exercise = await _context.Exercises
                .Include(e => e.ExerciseSessions)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise == null)
            {
                return NotFound($"Exercise with ID {id} not found");
            }

            // Check if exercise is being used in any exercise sessions
            if (exercise.ExerciseSessions.Any())
            {
                return Conflict("Cannot delete exercise that is referenced by exercise sessions");
            }

            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exercise with ID {ExerciseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get exercise statistics (Admin only)
    /// </summary>
    /// <returns>Exercise usage statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExerciseStatistics()
    {
        try
        {
            var totalExercises = await _context.Exercises.CountAsync();
            var exercisesByMuscleGroup = await _context.Exercises
                .GroupBy(e => e.MuscleGroup)
                .Select(g => new { MuscleGroup = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var exercisesByDifficulty = await _context.Exercises
                .GroupBy(e => e.Difficulty)
                .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                .ToListAsync();

            var mostUsedExercises = await _context.ExerciseSessions
                .GroupBy(es => es.ExerciseId)
                .Select(g => new { 
                    ExerciseId = g.Key, 
                    UsageCount = g.Count(),
                    ExerciseName = g.First().Exercise.Name
                })
                .OrderByDescending(x => x.UsageCount)
                .Take(10)
                .ToListAsync();

            var statistics = new
            {
                TotalExercises = totalExercises,
                ExercisesByMuscleGroup = exercisesByMuscleGroup,
                ExercisesByDifficulty = exercisesByDifficulty,
                MostUsedExercises = mostUsedExercises
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercise statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}
