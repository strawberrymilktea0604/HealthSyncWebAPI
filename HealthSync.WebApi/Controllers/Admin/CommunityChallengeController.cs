using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers.Admin;

/// <summary>
/// Admin Community Challenge Management Controller
/// Handles creation, modification, and approval of community challenges
/// </summary>
[ApiController]
[Route("api/v1/admin/challenges")]
[Authorize(Roles = "Admin")]
public class CommunityChallengeController : ControllerBase
{
    private readonly IChallengeAdminService _challengeAdminService;
    private readonly ILogger<CommunityChallengeController> _logger;

    public CommunityChallengeController(
        IChallengeAdminService challengeAdminService,
        ILogger<CommunityChallengeController> logger)
    {
        _challengeAdminService = challengeAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Create new community challenge
    /// </summary>
    /// <param name="request">Challenge creation request</param>
    /// <returns>201 Created with challenge data</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request)
    {
        try
        {
            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid input data", errors = ModelState.Values.SelectMany(v => v.Errors) });

            _logger.LogInformation($"Admin {adminId} creating new challenge: {request.Title}");

            var (success, data, message) = await _challengeAdminService.CreateChallengeAsync(request, adminId);

            if (!success)
            {
                _logger.LogWarning($"Failed to create challenge: {message}");
                return BadRequest(new { success = false, message });
            }

            _logger.LogInformation($"Challenge created successfully by admin {adminId}");
            return CreatedAtAction(nameof(GetChallenge), new { id = data?.ChallengeId }, 
                new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating challenge: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the challenge" });
        }
    }

    /// <summary>
    /// Get challenge by ID
    /// </summary>
    /// <param name="id">Challenge ID</param>
    /// <returns>200 OK with challenge data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChallenge(int id)
    {
        try
        {
            var (success, data, message) = await _challengeAdminService.GetChallengeAsync(id);

            if (!success)
            {
                _logger.LogWarning($"Challenge {id} not found");
                return NotFound(new { success = false, message });
            }

            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving challenge {id}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the challenge" });
        }
    }

    /// <summary>
    /// Get all challenges with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>200 OK with paginated challenges</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllChallenges([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (success, data, message) = await _challengeAdminService.GetAllChallengesAsync(page, pageSize);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving challenges: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving challenges" });
        }
    }

    /// <summary>
    /// Update challenge
    /// </summary>
    /// <param name="id">Challenge ID</param>
    /// <param name="request">Update request with optional fields</param>
    /// <returns>200 OK with updated challenge</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateChallenge(int id, [FromBody] UpdateChallengeRequest request)
    {
        try
        {
            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            _logger.LogInformation($"Admin {adminId} updating challenge {id}");

            var (success, data, message) = await _challengeAdminService.UpdateChallengeAsync(id, request, adminId);

            if (!success)
            {
                _logger.LogWarning($"Failed to update challenge {id}: {message}");
                return NotFound(new { success = false, message });
            }

            _logger.LogInformation($"Challenge {id} updated successfully by admin {adminId}");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating challenge {id}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while updating the challenge" });
        }
    }

    /// <summary>
    /// Delete challenge
    /// </summary>
    /// <param name="id">Challenge ID</param>
    /// <returns>204 No Content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        try
        {
            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            _logger.LogInformation($"Admin {adminId} deleting challenge {id}");

            var (success, message) = await _challengeAdminService.DeleteChallengeAsync(id, adminId);

            if (!success)
            {
                _logger.LogWarning($"Failed to delete challenge {id}: {message}");
                return NotFound(new { success = false, message });
            }

            _logger.LogInformation($"Challenge {id} deleted successfully by admin {adminId}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting challenge {id}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the challenge" });
        }
    }

    /// <summary>
    /// Get pending approvals for a challenge
    /// </summary>
    /// <param name="challengeId">Challenge ID</param>
    /// <returns>200 OK with pending participations</returns>
    [HttpGet("{challengeId}/pending-approvals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingApprovals(int challengeId)
    {
        try
        {
            var (success, data, message) = await _challengeAdminService.GetPendingApprovalsAsync(challengeId);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving pending approvals for challenge {challengeId}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving pending approvals" });
        }
    }

    /// <summary>
    /// Review participation submission (approve/reject)
    /// </summary>
    /// <param name="participationId">Participation ID</param>
    /// <param name="request">Review decision (approved/rejected with notes)</param>
    /// <returns>200 OK with updated participation</returns>
    [HttpPost("participations/{participationId}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReviewParticipation(int participationId, [FromBody] ReviewParticipationRequest request)
    {
        try
        {
            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (adminId == 0)
                return Unauthorized(new { success = false, message = "Invalid admin ID" });

            _logger.LogInformation($"Admin {adminId} reviewing participation {participationId}");

            var (success, data, message) = await _challengeAdminService.ReviewParticipationAsync(participationId, request, adminId);

            if (!success)
            {
                _logger.LogWarning($"Failed to review participation {participationId}: {message}");
                return NotFound(new { success = false, message });
            }

            _logger.LogInformation($"Participation {participationId} reviewed successfully by admin {adminId}");
            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reviewing participation {participationId}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while reviewing the participation" });
        }
    }

    /// <summary>
    /// Get all participants for a challenge
    /// </summary>
    /// <param name="challengeId">Challenge ID</param>
    /// <returns>200 OK with participants list</returns>
    [HttpGet("{challengeId}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChallengeParticipants(int challengeId)
    {
        try
        {
            var (success, data, message) = await _challengeAdminService.GetChallengeParticipantsAsync(challengeId);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, data, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving participants for challenge {challengeId}: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving participants" });
        }
    }
}
