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
    /// Set or update user's rank title (e.g., "Top Contributor")
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Request containing rank title (can be null to clear title)</param>
    /// <returns>200 OK with updated user rank information, 404 Not Found if user doesn't exist</returns>
    [HttpPost("users/{id}/set-title")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetUserRankTitle(int id, [FromBody] SetUserRankTitleRequest request)
    {
        try
        {
            _logger.LogInformation($"[AdminController] Setting rank title for user {id} to '{request.RankTitle}'");

            var result = await _userService.SetUserRankTitleAsync(id, request.RankTitle);

            if (result is null)
            {
                _logger.LogWarning($"[AdminController] User {id} not found for rank title update");
                return NotFound(new { success = false, message = "User not found" });
            }

            _logger.LogInformation($"[AdminController] Rank title updated successfully for user {id}");
            return Ok(new { success = true, data = result, message = "User rank title updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AdminController] Error setting user rank title: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while updating user rank title", error = ex.Message });
        }
    }
}