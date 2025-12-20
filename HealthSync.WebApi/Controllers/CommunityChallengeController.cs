using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Domain.Entities;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/challenges")]
[Authorize(Roles = "Customer")]
public class CommunityChallengeController : ControllerBase
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeParticipationRepository _participationRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly HealthSync.Application.Interfaces.INotificationRepository _notificationRepository;

    private const string ErrorOccurredMessage = "An error occurred";

    public CommunityChallengeController(
        IChallengeRepository challengeRepository,
        IChallengeParticipationRepository participationRepository,
        IFileStorageService fileStorage,
        HealthSync.Application.Interfaces.INotificationRepository notificationRepository)
    {
        _challengeRepository = challengeRepository;
        _participationRepository = participationRepository;
        _fileStorage = fileStorage;
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// Get list of open challenges
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOpenChallenges([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var (challenges, totalCount) = await _challengeRepository.GetAllAsync(page, pageSize);
            var openChallenges = challenges.Where(c => c.Status == ChallengeStatus.Open).ToList();

            var result = openChallenges.Select(c => new ChallengeDto
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
                CurrentParticipants = c.Participations?.Count ?? 0,
                RewardDescription = c.RewardDescription,
                ImageUrl = c.ImageUrl,
                CreatedByAdminId = c.CreatedByAdminId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                data = result,
                pagination = new
                {
                    page,
                    pageSize,
                    totalItems = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's joined challenges
    /// </summary>
    [HttpGet("my-challenges")]
    public async Task<IActionResult> GetMyChallenges()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { success = false, message = "Invalid user" });

            var userParticipations = await _participationRepository.GetByUserIdAsync(userId);

            var result = userParticipations.Select(p => new
            {
                challenge = new ChallengeDto
                {
                    ChallengeId = p.Challenge.ChallengeId,
                    Title = p.Challenge.Title,
                    Description = p.Challenge.Description,
                    ChallengeType = p.Challenge.ChallengeType,
                    StartDate = p.Challenge.StartDate,
                    EndDate = p.Challenge.EndDate,
                    Criteria = p.Challenge.Criteria,
                    Status = p.Challenge.Status,
                    MaxParticipants = p.Challenge.MaxParticipants,
                    CurrentParticipants = p.Challenge.Participations?.Count ?? 0,
                    RewardDescription = p.Challenge.RewardDescription,
                    ImageUrl = p.Challenge.ImageUrl,
                    CreatedByAdminId = p.Challenge.CreatedByAdminId,
                    CreatedAt = p.Challenge.CreatedAt,
                    UpdatedAt = p.Challenge.UpdatedAt
                },
                participation = new ParticipationDto
                {
                    ParticipationId = p.ParticipationId,
                    ChallengeId = p.ChallengeId,
                    UserId = p.UserId,
                    JoinedDate = p.JoinedDate,
                    Status = p.Status,
                    SubmissionText = p.SubmissionText,
                    SubmissionUrl = p.SubmissionUrl,
                    SubmittedAt = p.SubmittedAt,
                    ReviewedByAdminId = p.ReviewedByAdminId,
                    ReviewDate = p.ReviewDate,
                    ReviewNotes = p.ReviewNotes,
                    CompletedAt = p.CompletedAt,
                    CreatedAt = p.CreatedAt
                }
            }).ToList();

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Join a challenge (create participation with status = Joined)
    /// </summary>
    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { success = false, message = "Invalid user" });

            var challenge = await _challengeRepository.GetByIdWithParticipationsAsync(id);
            if (challenge == null)
                return NotFound(new { success = false, message = "Challenge not found" });

            if (challenge.Status != ChallengeStatus.Open)
                return BadRequest(new { success = false, message = "Challenge is not open" });

            // Check if already joined
            var already = await _participationRepository.IsUserParticipatedAsync(id, userId);
            if (already)
                return BadRequest(new { success = false, message = "Already joined this challenge" });

            // Check max participants
            if (challenge.MaxParticipants.HasValue)
            {
                var count = await _participationRepository.GetParticipantCountAsync(id);
                if (count >= challenge.MaxParticipants.Value)
                    return BadRequest(new { success = false, message = "Challenge is full" });
            }

            var participation = new ChallengeParticipation
            {
                ChallengeId = id,
                UserId = userId,
                JoinedDate = DateTime.UtcNow,
                Status = ParticipationStatus.Joined,
                CreatedAt = DateTime.UtcNow
            };

            await _participationRepository.AddAsync(participation);
            await _participationRepository.SaveChangesAsync();

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
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a challenge result (sets status = PendingApproval)
    /// </summary>
    [HttpPost("submit/{participationId}")]
    public async Task<IActionResult> Submit(int participationId, [FromForm] SubmitChallengeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { success = false, message = "Invalid user" });

            var participation = await _participationRepository.GetByIdWithDetailsAsync(participationId);
            if (participation == null || participation.UserId != userId)
                return NotFound(new { success = false, message = "Participation not found" });

            // Ensure challenge still open and within time window
            var now = DateTime.UtcNow;
            if (participation.Challenge.Status != ChallengeStatus.Open)
                return BadRequest(new { success = false, message = "Challenge is closed" });

            if (now < participation.Challenge.StartDate || now > participation.Challenge.EndDate)
                return BadRequest(new { success = false, message = "Challenge is not active" });

            if (participation.Status == ParticipationStatus.PendingApproval || participation.Status == ParticipationStatus.Completed)
                return BadRequest(new { success = false, message = "Challenge already submitted or completed" });

            string? imageUrl = null;
            if (request.SubmissionImage != null)
            {
                try
                {
                    imageUrl = await _fileStorage.UploadAsync(request.SubmissionImage, "challenge-submissions");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Failed to upload image", error = ex.Message });
                }
            }

            participation.SubmissionText = request.SubmissionText;
            participation.SubmissionUrl = imageUrl;
            participation.SubmittedAt = DateTime.UtcNow;
            participation.Status = ParticipationStatus.PendingApproval;

            await _participationRepository.UpdateAsync(participation);
            await _participationRepository.SaveChangesAsync();

            // Create in-app notification for Admins
            var notif = new HealthSync.Domain.Entities.Notification
            {
                Title = "New challenge submission",
                Message = $"User {participation.UserId} submitted for challenge '{participation.Challenge.Title}'.",
                RecipientRole = "Admin",
                RelatedEntityId = participation.ParticipationId,
                RelatedEntityType = "ChallengeParticipation",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notif);
            await _notificationRepository.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Submission received; pending admin approval",
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
            return StatusCode(500, new { success = false, message = ErrorOccurredMessage, error = ex.Message });
        }
    }
}
