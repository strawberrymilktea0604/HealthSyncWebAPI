using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/challenges")]
[Authorize(Roles = "Customer")]
public class ChallengeParticipationController : ControllerBase
{
    private readonly IChallengeParticipationService _participationService;

    public ChallengeParticipationController(IChallengeParticipationService participationService)
    {
        _participationService = participationService;
    }

    /// <summary>
    /// Submit challenge result
    /// </summary>
    [HttpPost("{challengeId}/submit")]
    public async Task<IActionResult> SubmitChallenge(int challengeId, [FromForm] SubmitChallengeRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var result = await _participationService.SubmitChallengeResultAsync(challengeId, userId, request);

            return Ok(new { success = true, data = result, message = "Challenge submission successful, waiting for admin approval" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's challenge participations
    /// </summary>
    [HttpGet("my-participations")]
    public async Task<IActionResult> GetMyParticipations()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var participations = await _participationService.GetUserParticipationsAsync(userId);

            return Ok(new { success = true, data = participations });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}