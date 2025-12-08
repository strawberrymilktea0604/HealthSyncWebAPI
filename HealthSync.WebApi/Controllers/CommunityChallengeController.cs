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
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
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
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}
