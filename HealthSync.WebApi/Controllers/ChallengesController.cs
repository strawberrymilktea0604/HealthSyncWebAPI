using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/challenges")]
[Authorize(Roles = "Customer")]
public class ChallengesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IChallengeParticipationService _participationService;

    public ChallengesController(
        ApplicationDbContext context, 
        IStorageService storageService,
        IChallengeParticipationService participationService)
    {
        _context = context;
        _storageService = storageService;
        _participationService = participationService;
    }

    /// <summary>
    /// Get all open challenges
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOpenChallenges(
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

            var query = _context.Challenges
                .Where(c => c.Status == ChallengeStatus.Open && c.EndDate >= DateTime.Now)
                .OrderByDescending(c => c.CreatedAt)
                .AsQueryable();

            var totalItems = await query.CountAsync();

            var challenges = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ChallengeDto
                {
                    ChallengeId = c.ChallengeId,
                    Title = c.Title,
                    Description = c.Description,
                    ChallengeType = c.ChallengeType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Criteria = c.Criteria,
                    Status = c.Status,
                    MaxParticipants = c.MaxParticipants,
                    CurrentParticipants = c.Participations.Count(),
                    RewardDescription = c.RewardDescription,
                    ImageUrl = c.ImageUrl,
                    CreatedByAdminId = c.CreatedByAdminId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            var result = new
            {
                items = challenges,
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get challenge details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChallenge(int id)
    {
        try
        {
            var challenge = await _context.Challenges
                .Where(c => c.ChallengeId == id)
                .Select(c => new ChallengeDto
                {
                    ChallengeId = c.ChallengeId,
                    Title = c.Title,
                    Description = c.Description,
                    ChallengeType = c.ChallengeType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Criteria = c.Criteria,
                    Status = c.Status,
                    MaxParticipants = c.MaxParticipants,
                    CurrentParticipants = c.Participations.Count(),
                    RewardDescription = c.RewardDescription,
                    ImageUrl = c.ImageUrl,
                    CreatedByAdminId = c.CreatedByAdminId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (challenge == null)
            {
                return NotFound(new { success = false, message = "Challenge not found" });
            }

            return Ok(new { success = true, data = challenge });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
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
