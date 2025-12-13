using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/progress-records")]
[Authorize(Roles = "Customer")]
public class ProgressRecordsController : ControllerBase
{
    private readonly IGoalService _goalService;
    
    private const string InvalidUserMessage = "Invalid user";
    private const string ErrorOccurredMessage = "An error occurred";
    private const string ProgressRecordNotFoundMessage = "Progress record not found";

    public ProgressRecordsController(IGoalService goalService)
    {
        _goalService = goalService;
    }

    /// <summary>
    /// Create a new progress record
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProgressRecord([FromBody] CreateProgressRecordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            var progressRecord = await _goalService.RecordProgressAsync(new RecordProgressRequest
            {
                GoalId = request.GoalId,
                RecordDate = request.RecordDate,
                RecordedValue = request.RecordedValue,
                WeightKg = request.WeightKg,
                WaistCm = request.WaistCm,
                ChestCm = request.ChestCm,
                HipCm = request.HipCm,
                Notes = request.Notes
            }, userId);

            return CreatedAtAction(nameof(GetProgressRecord), new { id = progressRecord.ProgressRecordId },
                new { success = true, data = progressRecord, message = "Progress record created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific progress record by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgressRecord(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            var progressRecord = await _goalService.GetProgressRecordAsync(id, userId);

            if (progressRecord == null)
            {
                return NotFound(new { success = false, message = ProgressRecordNotFoundMessage });
            }

            return Ok(new { success = true, data = progressRecord });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all progress records for a specific goal
    /// </summary>
    [HttpGet("goal/{goalId}")]
    public async Task<IActionResult> GetProgressRecordsByGoal(int goalId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            var progressRecords = await _goalService.GetProgressRecordsByGoalAsync(goalId, userId);

            return Ok(new { success = true, data = progressRecords });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Update a progress record
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProgressRecord(int id, [FromBody] CreateProgressRecordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            await _goalService.UpdateProgressRecordAsync(id, new UpdateProgressRequest
            {
                RecordedValue = request.RecordedValue,
                WeightKg = request.WeightKg,
                WaistCm = request.WaistCm,
                ChestCm = request.ChestCm,
                HipCm = request.HipCm,
                Notes = request.Notes
            }, userId);

            return Ok(new { success = true, message = "Progress record updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a progress record
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProgressRecord(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            await _goalService.DeleteProgressRecordAsync(id, userId);

            return Ok(new { success = true, message = "Progress record deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Get progress chart data (date, weight) for current user.
    /// </summary>
    [HttpGet("chart")]
    public async Task<IActionResult> GetProgressChart()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = InvalidUserMessage });
            }

            var chartData = await _goalService.GetUserProgressChartAsync(userId);

            return Ok(new { success = true, data = chartData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }
}
