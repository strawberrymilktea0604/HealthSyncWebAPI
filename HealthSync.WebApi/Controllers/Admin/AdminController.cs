using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDashboardAdminService _dashboardService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserService userService,
        IDashboardAdminService dashboardService,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        try
        {
            await _userService.UpdateUserStatusAsync(id, request.IsActive);
            return Ok(new { success = true, message = "User status updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "User not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        try
        {
            await _userService.UpdateUserRoleAsync(id, request.Role);
            return Ok(new { success = true, message = "User role updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "User not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get main dashboard statistics (3 key metrics)
    /// - Total active users
    /// - New users this month
    /// - Total workouts logged today
    /// </summary>
    /// <returns>200 OK with dashboard statistics</returns>
    [HttpGet("dashboard/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            _logger.LogInformation("[AdminController] Getting dashboard statistics");

            var (success, data, message) = await _dashboardService.GetDashboardStatsAsync();

            if (!success)
            {
                _logger.LogWarning($"[AdminController] Failed to get dashboard stats: {message}");
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[AdminController] Dashboard statistics retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AdminController] Error getting dashboard stats: {ex.Message}");
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
    /// <returns>200 OK with detailed statistics</returns>
    [HttpGet("dashboard/stats/detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetailedDashboardStats()
    {
        try
        {
            _logger.LogInformation("[AdminController] Getting detailed dashboard statistics");

            var (success, data, message) = await _dashboardService.GetDetailedStatsAsync();

            if (!success)
            {
                _logger.LogWarning($"[AdminController] Failed to get detailed stats: {message}");
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[AdminController] Detailed statistics retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AdminController] Error getting detailed stats: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving detailed statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get top content (top 5 exercises and top 5 forum categories)
    /// </summary>
    /// <returns>200 OK with top content data</returns>
    [HttpGet("dashboard/top-content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopContent()
    {
        try
        {
            _logger.LogInformation("[AdminController] Getting top content (exercises and forum categories)");

            var (success, data, message) = await _dashboardService.GetTopContentAsync();

            if (!success)
            {
                _logger.LogWarning($"[AdminController] Failed to get top content: {message}");
                return StatusCode(500, new { success = false, message });
            }

            _logger.LogInformation("[AdminController] Top content retrieved successfully");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AdminController] Error getting top content: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving top content", error = ex.Message });
        }
    }
}