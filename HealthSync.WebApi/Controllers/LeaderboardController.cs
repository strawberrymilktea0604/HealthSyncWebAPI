using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Leaderboard;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/leaderboard")]
[Authorize(Roles = "Customer")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Get top users by contribution points (Customer view)
    /// </summary>
    [HttpGet("top")]
    public async Task<IActionResult> GetTopLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
            {
                return BadRequest(new { success = false, message = "Limit must be between 1 and 100" });
            }

            var topUsers = await _leaderboardService.GetTopUsersByContributionPointsAsync(limit);

            return Ok(new { success = true, data = topUsers });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's leaderboard ranking
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRanking()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var myRank = await _leaderboardService.GetUserRankAsync(userId);

            if (myRank == null)
            {
                return NotFound(new { success = false, message = "Leaderboard entry not found" });
            }

            return Ok(new { success = true, data = myRank });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get leaderboard with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLeaderboard(
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

            var result = await _leaderboardService.GetLeaderboardAsync(pageNumber, pageSize);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}
