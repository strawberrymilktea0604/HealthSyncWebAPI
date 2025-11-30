using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers.Admin;

/// <summary>
/// Admin controller for forum moderation operations
/// </summary>
[ApiController]
[Route("api/v1/admin/forum")]
[Authorize(Roles = "Admin")]
public class ForumModerationController : ControllerBase
{
    private readonly IForumAdminService _forumAdminService;
    private readonly ILogger<ForumModerationController> _logger;

    public ForumModerationController(
        IForumAdminService forumAdminService,
        ILogger<ForumModerationController> logger)
    {
        _forumAdminService = forumAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Toggle pin status of a post (pin if unpinned, unpin if pinned)
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <returns>Result with current pin status</returns>
    [HttpPut("posts/{id}/pin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TogglePinPost(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "Invalid admin user" });
            }

            var (success, message, isPinned) = await _forumAdminService.TogglePinPostAsync(id, adminId);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new { message });
                }
                return BadRequest(new { message });
            }

            return Ok(new { message, isPinned });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling pin status for post with ID {PostId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lock a post to prevent further replies (Admin only)
    /// </summary>
    /// <param name="id">Post ID</param>
    [HttpPut("posts/{id}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LockPost(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "Invalid admin user" });
            }

            var (success, message) = await _forumAdminService.LockPostAsync(id, adminId);

            if (!success)
            {
                if (message.Contains("not found"))
                    return NotFound(new { message });
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking post with ID {PostId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
