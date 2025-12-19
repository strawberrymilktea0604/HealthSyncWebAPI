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

    private const string InvalidAdminUserMessage = "Invalid admin user";

    public ForumModerationController(
        IForumAdminService forumAdminService,
        ILogger<ForumModerationController> logger)
    {
        _forumAdminService = forumAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Pin a post to top of category (Admin only)
    /// </summary>
    /// <param name="id">Post ID</param>
    [HttpPut("posts/{id}/pin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PinPost(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = InvalidAdminUserMessage });
            }

            var (success, message) = await _forumAdminService.PinPostAsync(id, adminId);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new { message });
                }
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning post with ID {PostId}", id);
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
                return Unauthorized(new { message = InvalidAdminUserMessage });
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

    /// <summary>
    /// Delete a post and all its replies (Admin only)
    /// </summary>
    /// <param name="id">Post ID</param>
    [HttpDelete("posts/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = InvalidAdminUserMessage });
            }

            var (success, message) = await _forumAdminService.DeletePostAsync(id, adminId);

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
            _logger.LogError(ex, "Error deleting post with ID {PostId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Hide a reply (set is_hidden = true) (Admin only)
    /// </summary>
    /// <param name="id">Reply ID</param>
    [HttpPut("replies/{id}/hide")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HideReply(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = InvalidAdminUserMessage });
            }

            var (success, message) = await _forumAdminService.HideReplyAsync(id, adminId);

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
            _logger.LogError(ex, "Error hiding reply with ID {ReplyId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a reply (Admin only)
    /// </summary>
    /// <param name="id">Reply ID</param>
    [HttpDelete("replies/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteReply(int id)
    {
        try
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = InvalidAdminUserMessage });
            }

            var (success, message) = await _forumAdminService.DeleteReplyAsync(id, adminId);

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
            _logger.LogError(ex, "Error deleting reply with ID {ReplyId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
