using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/workout-logs")]
[Authorize(Roles = "Customer")]
public class WorkoutLogsController : ControllerBase
{
    private readonly IWorkoutLogService _workoutLogService;
    private readonly ILogger<WorkoutLogsController> _logger;

    public WorkoutLogsController(IWorkoutLogService workoutLogService, ILogger<WorkoutLogsController> logger)
    {
        _workoutLogService = workoutLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new workout log with exercise sessions
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkoutLogResponse>> CreateWorkoutLog([FromBody] CreateWorkoutLogRequest request)
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
                return Unauthorized(new { success = false, message = "User ID not found in token" });
            }

            var response = await _workoutLogService.CreateWorkoutLogAsync(userId, request);

            return CreatedAtAction(nameof(CreateWorkoutLog), new { id = response.WorkoutLogId }, 
                new { success = true, data = response, message = "Workout log created successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger?.LogWarning(ex, "Invalid input for workout log creation");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating workout log");
            return StatusCode(500, new { success = false, message = "An error occurred while creating workout log", error = ex.Message });
        }
    }

    /// <summary>
    /// Get workout logs history for the current user with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetWorkoutLogs(
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
                return Unauthorized(new { success = false, message = "User ID not found in token" });
            }

            var result = await _workoutLogService.GetWorkoutLogsAsync(userId, pageNumber, pageSize);

            return Ok(new { success = true, data = result, message = "Workout logs retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving workout logs");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving workout logs", error = ex.Message });
        }
    }
}