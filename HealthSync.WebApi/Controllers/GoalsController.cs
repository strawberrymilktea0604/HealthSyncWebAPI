using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Validators.Goals;
using FluentValidation;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Customer")]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;

    public GoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }

    /// <summary>
    /// Create a new goal for the authenticated user
    /// </summary>
    /// <param name="request">Goal creation request</param>
    /// <returns>Created goal information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        // Validate request
        var validator = new CreateGoalValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var goal = await _goalService.CreateGoalAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetGoal), new { id = goal.GoalId }, new { success = true, data = goal });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while creating the goal" });
        }
    }

    /// <summary>
    /// Get a specific goal by ID
    /// </summary>
    /// <param name="id">Goal ID</param>
    /// <returns>Goal details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGoal(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var goal = await _goalService.GetGoalByIdAsync(id, userId.Value);
            return Ok(new { success = true, data = goal });
        }
        catch (ValidationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the goal" });
        }
    }

    /// <summary>
    /// Get all goals for the authenticated user
    /// </summary>
    /// <returns>List of user's goals</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGoals()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var goals = await _goalService.GetUserGoalsAsync(userId.Value);
            return Ok(new { success = true, data = goals });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving goals" });
        }
    }

    /// <summary>
    /// Update an existing goal
    /// </summary>
    /// <param name="id">Goal ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated goal</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateGoal(int id, [FromBody] UpdateGoalRequest request)
    {
        // Validate request
        var validator = new UpdateGoalValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var goal = await _goalService.UpdateGoalAsync(id, request, userId.Value);
            return Ok(new { success = true, data = goal });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the goal" });
        }
    }

    /// <summary>
    /// Delete a goal
    /// </summary>
    /// <param name="id">Goal ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteGoal(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            await _goalService.DeleteGoalAsync(id, userId.Value);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the goal" });
        }
    }

    /// <summary>
    /// Record progress for a goal
    /// </summary>
    /// <param name="id">Goal ID</param>
    /// <param name="request">Progress record request</param>
    /// <returns>Created progress record</returns>
    [HttpPost("{id}/progress")]
    [ProducesResponseType(typeof(ProgressRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordProgress(int id, [FromBody] RecordProgressRequest request)
    {
        // Validate request
        var validator = new RecordProgressValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var progressRecord = await _goalService.RecordProgressAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetProgressRecord), new { goalId = id, recordId = progressRecord.ProgressRecordId }, new { success = true, data = progressRecord });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting progress" });
        }
    }

    /// <summary>
    /// Get a specific progress record
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <param name="recordId">Progress record ID</param>
    /// <returns>Progress record details</returns>
    [HttpGet("{goalId}/progress/{recordId}")]
    [ProducesResponseType(typeof(ProgressRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProgressRecord(int goalId, int recordId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            // For now, we'll get the goal and find the progress record
            var goal = await _goalService.GetGoalByIdAsync(goalId, userId.Value);
            var progressRecord = goal.ProgressRecords?.FirstOrDefault(pr => pr.ProgressRecordId == recordId);

            if (progressRecord == null)
                return NotFound(new { success = false, message = "Progress record not found" });

            return Ok(new { success = true, data = progressRecord });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving progress records" });
        }
    }

    /// <summary>
    /// Update a progress record
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <param name="recordId">Progress record ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated progress record</returns>
    [HttpPut("{goalId}/progress/{recordId}")]
    [ProducesResponseType(typeof(ProgressRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProgressRecord(int goalId, int recordId, [FromBody] UpdateProgressRequest request)
    {
        // Validate request
        var validator = new UpdateProgressValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var progressRecord = await _goalService.UpdateProgressRecordAsync(recordId, request, userId.Value);
            return Ok(new { success = true, data = progressRecord });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating the goal" });
        }
    }

    /// <summary>
    /// Delete a progress record
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <param name="recordId">Progress record ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{goalId}/progress/{recordId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProgressRecord(int goalId, int recordId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            await _goalService.DeleteProgressRecordAsync(recordId, userId.Value);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the progress record" });
        }
    }

    /// <summary>
    /// Get progress chart data for a goal
    /// </summary>
    /// <param name="id">Goal ID</param>
    /// <returns>Chart data for progress visualization</returns>
    [HttpGet("{id}/chart")]
    [ProducesResponseType(typeof(ChartDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProgressChart(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new { success = false, message = "User not authenticated" });

        try
        {
            var chartData = await _goalService.GetProgressChartAsync(id, userId.Value);
            return Ok(new { success = true, data = chartData });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving chart data" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}