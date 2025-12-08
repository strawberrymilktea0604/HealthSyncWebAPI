using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

/// <summary>
/// Admin Dashboard Controller
/// Provides endpoints for admin dashboard statistics and analytics
/// </summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardAdminService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardAdminService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get main dashboard statistics (3 key metrics)
    /// - Total active users
    /// - New users this month
    /// - Total workouts logged today
    /// </summary>
    /// <returns>200 OK with dashboard statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            _logger.LogInformation("[DashboardController] Getting dashboard statistics");

            var (success, data, message) = await _dashboardService.GetDashboardStatsAsync();

            if (!success)
            {
                _logger.LogWarning("[DashboardController] Failed to get dashboard stats: {Message}", message);
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[DashboardController] Dashboard statistics retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardController] Error getting dashboard stats");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving dashboard statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed dashboard statistics (extended metrics)
    /// - Total active users
    /// - New users this month
    /// - Workouts logged today
    /// - Nutrition logs today
    /// - Forum posts this month
    /// - Forum replies this month
    /// - Open challenges
    /// - Pending challenge submissions
    /// </summary>
    /// <returns>200 OK with detailed dashboard statistics</returns>
    [HttpGet("stats/detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetailedStats()
    {
        try
        {
            _logger.LogInformation("[DashboardController] Getting detailed dashboard statistics");

            var (success, data, message) = await _dashboardService.GetDetailedStatsAsync();

            if (!success)
            {
                _logger.LogWarning("[DashboardController] Failed to get detailed stats: {Message}", message);
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[DashboardController] Detailed statistics retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardController] Error getting detailed stats");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving detailed statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get top content (top 5 exercises and top 5 forum categories)
    /// - Top 5 exercises by usage count
    /// - Top 5 forum categories by activity (posts + replies)
    /// </summary>
    /// <returns>200 OK with top content data</returns>
    [HttpGet("top-content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopContent()
    {
        try
        {
            _logger.LogInformation("[DashboardController] Getting top content (exercises and forum categories)");

            var (success, data, message) = await _dashboardService.GetTopContentAsync();

            if (!success)
            {
                _logger.LogWarning("[DashboardController] Failed to get top content: {Message}", message);
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[DashboardController] Top content retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardController] Error getting top content");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving top content", error = ex.Message });
        }
    }
}
