using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ExercisesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/exercises/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExercise(int id)
    {
        var exercise = await _db.Exercises.FindAsync(id);
        if (exercise is null) return NotFound(new { success = false, message = "Exercise not found" });

        return Ok(new
        {
            success = true,
            data = new
            {
                exercise.ExerciseId,
                exercise.Name,
                exercise.MuscleGroup,
                exercise.Difficulty,
                exercise.Equipment,
                exercise.Description,
                exercise.ImageUrl
            }
        });
    }

    // POST: api/exercises
    // Admin-only: create a new exercise
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        if (request is null) return BadRequest(new { success = false, message = "Request body is required" });

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MuscleGroup) || string.IsNullOrWhiteSpace(request.Difficulty))
            return BadRequest(new { success = false, message = "Name, MuscleGroup and Difficulty are required" });

        var exercise = new Exercise
        {
            Name = request.Name.Trim(),
            MuscleGroup = request.MuscleGroup.Trim(),
            Difficulty = request.Difficulty.Trim(),
            Equipment = string.IsNullOrWhiteSpace(request.Equipment) ? null : request.Equipment.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        // Return created resource
        return CreatedAtAction(nameof(GetExercise), new { id = exercise.ExerciseId }, new { success = true, data = exercise });
    }

    // PUT: api/exercises/5
    // Admin-only: update existing exercise
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateExercise(int id, [FromBody] UpdateExerciseRequest request)
    {
        if (request is null) return BadRequest(new { success = false, message = "Request body is required" });

        var exercise = await _db.Exercises.FindAsync(id);
        if (exercise is null) return NotFound(new { success = false, message = "Exercise not found" });

        // Apply updates (only set values when provided)
        if (!string.IsNullOrWhiteSpace(request.Name)) exercise.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.MuscleGroup)) exercise.MuscleGroup = request.MuscleGroup.Trim();
        if (!string.IsNullOrWhiteSpace(request.Difficulty)) exercise.Difficulty = request.Difficulty.Trim();

        exercise.Equipment = request.Equipment is null ? exercise.Equipment : (string.IsNullOrWhiteSpace(request.Equipment) ? null : request.Equipment.Trim());
        exercise.Description = request.Description is null ? exercise.Description : (string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
        exercise.ImageUrl = request.ImageUrl is null ? exercise.ImageUrl : (string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim());

        _db.Exercises.Update(exercise);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = exercise });
    }

    // GET: api/exercises
    // Optionally filtered by muscleGroup and/or difficulty, with pagination
    [HttpGet]
    public async Task<IActionResult> GetExercises(
        [FromQuery] string? muscleGroup,
        [FromQuery] string? difficulty,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        // Start with base query
        var query = _db.Exercises.AsQueryable();

        // Apply filters if provided
        if (!string.IsNullOrWhiteSpace(muscleGroup))
            query = query.Where(e => e.MuscleGroup.ToLower() == muscleGroup.ToLower());
        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(e => e.Difficulty.ToLower() == difficulty.ToLower());

        // Get total count for pagination metadata
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Apply pagination and execute query
        var exercises = await query
            .OrderBy(e => e.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Return paginated result with metadata
        return Ok(new
        {
            success = true,
            data = exercises,
            pagination = new
            {
                currentPage = pageNumber,
                pageSize,
                totalItems,
                totalPages,
                hasNext = pageNumber < totalPages,
                hasPrevious = pageNumber > 1
            }
        });
    }

    // DELETE: api/exercises/5
    // Admin-only: delete an exercise
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var exercise = await _db.Exercises.FindAsync(id);
        if (exercise is null) return NotFound(new { success = false, message = "Exercise not found" });

        // Check if exercise is used in any workout sessions before deleting
        var hasWorkouts = await _db.ExerciseSessions.AnyAsync(es => es.ExerciseId == id);
        if (hasWorkouts)
            return BadRequest(new { success = false, message = "Cannot delete exercise: it is used in workout logs" });

        _db.Exercises.Remove(exercise);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Exercise deleted successfully" });
    }

    // DTOs
    public record CreateExerciseRequest(string Name, string MuscleGroup, string Difficulty, string? Equipment, string? Description, string? ImageUrl);
    public record UpdateExerciseRequest(string? Name, string? MuscleGroup, string? Difficulty, string? Equipment, string? Description, string? ImageUrl);
}
