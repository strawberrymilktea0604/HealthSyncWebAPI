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

    public ChallengesController(ApplicationDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
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
                .Where(c => c.Status == ChallengeStatus.Open)
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
    /// Submit challenge result with optional image
    /// </summary>
    [HttpPost("submit/{submissionId}")]
    public async Task<IActionResult> SubmitChallenge(int submissionId, [FromForm] SubmitChallengeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Find participation
            var participation = await _context.ChallengeParticipations
                .Include(p => p.Challenge)
                .FirstOrDefaultAsync(p => p.ParticipationId == submissionId && p.UserId == userId);

            if (participation == null)
            {
                return NotFound(new { success = false, message = "Participation not found" });
            }

            // Check if challenge is still open
            if (participation.Challenge.Status != ChallengeStatus.Open)
            {
                return BadRequest(new { success = false, message = "Challenge is closed" });
            }

            // Check if within challenge period
            var now = DateTime.UtcNow;
            if (now < participation.Challenge.StartDate || now > participation.Challenge.EndDate)
            {
                return BadRequest(new { success = false, message = "Challenge is not active" });
            }

            // Check if already submitted
            if (participation.Status == ParticipationStatus.PendingApproval || 
                participation.Status == ParticipationStatus.Completed)
            {
                return BadRequest(new { success = false, message = "Challenge already submitted" });
            }

            // Upload image if provided
            string? imageUrl = null;
            if (request.SubmissionImage != null)
            {
                try
                {
                    imageUrl = await _storageService.UploadAsync(
                        request.SubmissionImage,
                        "challenge-submissions",
                        null
                    );
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Failed to upload image", error = ex.Message });
                }
            }

            // Update participation
            participation.SubmissionText = request.SubmissionText;
            participation.SubmissionUrl = imageUrl;
            participation.SubmittedAt = DateTime.UtcNow;
            participation.Status = ParticipationStatus.PendingApproval;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Challenge submission successful",
                data = new
                {
                    participationId = participation.ParticipationId,
                    submissionText = participation.SubmissionText,
                    submissionUrl = participation.SubmissionUrl,
                    submittedAt = participation.SubmittedAt,
                    status = participation.Status.ToString()
                }
            });
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
    /// Join a challenge
    /// </summary>
    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinChallenge(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Check if challenge exists and is open
            var challenge = await _context.Challenges.FindAsync(id);
            if (challenge == null)
            {
                return NotFound(new { success = false, message = "Challenge not found" });
            }

            if (challenge.Status != ChallengeStatus.Open)
            {
                return BadRequest(new { success = false, message = "Challenge is not open" });
            }

            // Check if already joined
            var existingParticipation = await _context.ChallengeParticipations
                .FirstOrDefaultAsync(p => p.ChallengeId == id && p.UserId == userId);

            if (existingParticipation != null)
            {
                return BadRequest(new { success = false, message = "Already joined this challenge" });
            }

            // Check max participants
            if (challenge.MaxParticipants.HasValue)
            {
                var currentCount = await _context.ChallengeParticipations
                    .CountAsync(p => p.ChallengeId == id);

                if (currentCount >= challenge.MaxParticipants.Value)
                {
                    return BadRequest(new { success = false, message = "Challenge is full" });
                }
            }

            // Create participation
            var participation = new ChallengeParticipation
            {
                ChallengeId = id,
                UserId = userId,
                JoinedDate = DateTime.UtcNow,
                Status = ParticipationStatus.Joined,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChallengeParticipations.Add(participation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Joined challenge successfully",
                data = new
                {
                    participationId = participation.ParticipationId,
                    challengeId = participation.ChallengeId,
                    joinedDate = participation.JoinedDate,
                    status = participation.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}
