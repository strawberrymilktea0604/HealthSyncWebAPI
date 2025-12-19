using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/leaderboard")]
[Authorize(Roles = "Admin")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardUpdateJob _leaderboardUpdateJob;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardUpdateJob leaderboardUpdateJob,
        ILogger<LeaderboardController> logger)
    {
        _leaderboardUpdateJob = leaderboardUpdateJob;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger leaderboard update job
    /// Updates all users' contribution points from Leaderboard to UserProfile
    /// </summary>
    /// <returns>200 OK if job starts successfully</returns>
    [HttpPost("update-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAllUserContributionPoints()
    {
        try
        {
            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            _logger.LogInformation("[Admin {AdminId}] Triggering leaderboard update job for all users", adminId);

            await _leaderboardUpdateJob.UpdateUserContributionPointsAsync();

            _logger.LogInformation("[Admin {AdminId}] Leaderboard update job completed successfully", adminId);
            return Ok(new
            {
                success = true,
                message = "Leaderboard update job executed successfully. All users' contribution points have been synced from Leaderboard to UserProfile."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing leaderboard update job: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while executing the leaderboard update job",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Manually trigger leaderboard update job for a specific user
    /// Updates a user's contribution points from Leaderboard to UserProfile
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <returns>200 OK if job starts successfully</returns>
    [HttpPost("update/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUserContributionPoints(int userId)
    {
        try
        {
            if (userId <= 0)
                return BadRequest(new { success = false, message = "Invalid user ID" });

            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            _logger.LogInformation("[Admin {AdminId}] Triggering leaderboard update job for UserId {UserId}", adminId, userId);

            await _leaderboardUpdateJob.UpdateUserContributionPointsAsync(userId);

            _logger.LogInformation("[Admin {AdminId}] Leaderboard update job for UserId {UserId} completed successfully", adminId, userId);
            return Ok(new
            {
                success = true,
                message = $"Leaderboard update job executed successfully for user {userId}. Contribution points have been synced from Leaderboard to UserProfile."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing leaderboard update job for UserId {UserId}: {Message}", userId, ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while executing the leaderboard update job",
                error = ex.Message
            });
        }
    }
}
