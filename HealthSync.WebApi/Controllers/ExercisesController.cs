using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly IExerciseService _exerciseService;

    public ExercisesController(IExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
    }

    // GET: api/exercises/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(int id)
    {
        try
        {
            var exercise = await _exerciseService.GetExerciseByIdAsync(id);
            return Ok(new { success = true, data = exercise });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Exercise not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the exercise", error = ex.Message });
        }
    }

    // POST: api/exercises
    // Admin-only: create a new exercise
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExerciseDto>> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var exercise = await _exerciseService.CreateExerciseAsync(request);
            return CreatedAtAction(nameof(GetExercise), new { id = exercise.Id }, new { success = true, data = exercise });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while creating the exercise", error = ex.Message });
        }
    }

    // PUT: api/exercises/5
    // Admin-only: update existing exercise
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(int id, [FromBody] UpdateExerciseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var exercise = await _exerciseService.UpdateExerciseAsync(id, request);
            return Ok(new { success = true, data = exercise });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Exercise not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating the exercise", error = ex.Message });
        }
    }

    // GET: api/exercises
    // Customer and Admin can view exercises with optional filtering and pagination
    [HttpGet]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<PaginatedResult<ExerciseDto>>> GetExercises(
        [FromQuery] string? muscleGroup,
        [FromQuery] string? difficulty,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _exerciseService.GetExercisesAsync(muscleGroup, difficulty, pageNumber, pageSize);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving exercises", error = ex.Message });
        }
    }

    // DELETE: api/exercises/5
    // Admin-only: delete an exercise
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        try
        {
            await _exerciseService.DeleteExerciseAsync(id);
            return Ok(new { success = true, message = "Exercise deleted successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Exercise not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the exercise", error = ex.Message });
        }
    }
}
