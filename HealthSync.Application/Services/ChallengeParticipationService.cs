using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class ChallengeParticipationService : IChallengeParticipationService
{
    private readonly IChallengeParticipationRepository _participationRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStorageService _storageService;

    public ChallengeParticipationService(
        IChallengeParticipationRepository participationRepository,
        IChallengeRepository challengeRepository,
        IUserRepository userRepository,
        IStorageService storageService)
    {
        _participationRepository = participationRepository;
        _challengeRepository = challengeRepository;
        _userRepository = userRepository;
        _storageService = storageService;
    }

    public async Task<ParticipationDto> JoinChallengeAsync(int challengeId, int userId)
    {
        // Check if challenge exists and is open
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge == null)
        {
            throw new ArgumentException("Challenge not found");
        }

        if (challenge.Status != ChallengeStatus.Open)
        {
            throw new InvalidOperationException("Challenge is not open for participation");
        }

        if (challenge.EndDate < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Challenge has ended");
        }

        // Check if user already participated
        var existingParticipation = await _participationRepository.GetUserParticipationAsync(challengeId, userId);
        if (existingParticipation != null)
        {
            throw new InvalidOperationException("You have already joined this challenge");
        }

        // Check max participants limit
        if (challenge.MaxParticipants.HasValue)
        {
            var currentCount = await _participationRepository.GetParticipantCountAsync(challengeId);
            if (currentCount >= challenge.MaxParticipants.Value)
            {
                throw new InvalidOperationException("Challenge has reached maximum participants");
            }
        }

        // Create new participation
        var participation = new ChallengeParticipation
        {
            ChallengeId = challengeId,
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            Status = ParticipationStatus.Joined
        };

        var addedParticipation = await _participationRepository.AddAsync(participation);
        await _participationRepository.SaveChangesAsync();

        // Return DTO
        return new ParticipationDto
        {
            ParticipationId = addedParticipation.ParticipationId,
            ChallengeId = addedParticipation.ChallengeId,
            UserId = addedParticipation.UserId,
            JoinedDate = addedParticipation.JoinedDate,
            Status = addedParticipation.Status,
            SubmissionText = addedParticipation.SubmissionText,
            SubmissionUrl = addedParticipation.SubmissionUrl,
            SubmittedAt = addedParticipation.SubmittedAt,
            ReviewedByAdminId = addedParticipation.ReviewedByAdminId,
            ReviewDate = addedParticipation.ReviewDate,
            ReviewNotes = addedParticipation.ReviewNotes,
            CompletedAt = addedParticipation.CompletedAt,
            CreatedAt = addedParticipation.JoinedDate
        };
    }

    public async Task<ParticipationDto> SubmitChallengeResultAsync(int challengeId, int userId, SubmitChallengeRequest request)
    {
        // Get user's participation
        var participation = await _participationRepository.GetUserParticipationAsync(challengeId, userId);
        if (participation == null)
        {
            throw new ArgumentException("You have not joined this challenge");
        }

        if (participation.Status != ParticipationStatus.Joined)
        {
            throw new InvalidOperationException("You can only submit once you have joined the challenge");
        }

        string? submissionUrl = null;
        if (request.SubmissionImage != null)
        {
            // Upload image to storage
            submissionUrl = await _storageService.UploadAsync(request.SubmissionImage, "challenge-submissions");
        }

        // Update participation
        participation.Status = ParticipationStatus.PendingApproval;
        participation.SubmissionText = request.SubmissionText;
        participation.SubmissionUrl = submissionUrl;
        participation.SubmittedAt = DateTime.UtcNow;

        await _participationRepository.UpdateAsync(participation);
        await _participationRepository.SaveChangesAsync();

        // Return updated DTO
        return new ParticipationDto
        {
            ParticipationId = participation.ParticipationId,
            ChallengeId = participation.ChallengeId,
            UserId = participation.UserId,
            JoinedDate = participation.JoinedDate,
            Status = participation.Status,
            SubmissionText = participation.SubmissionText,
            SubmissionUrl = participation.SubmissionUrl,
            SubmittedAt = participation.SubmittedAt,
            ReviewedByAdminId = participation.ReviewedByAdminId,
            ReviewDate = participation.ReviewDate,
            ReviewNotes = participation.ReviewNotes,
            CompletedAt = participation.CompletedAt,
            CreatedAt = participation.JoinedDate
        };
    }

    public async Task<IEnumerable<ParticipationDto>> GetUserParticipationsAsync(int userId)
    {
        var participations = await _participationRepository.GetByUserIdAsync(userId);

        return participations.Select(p => new ParticipationDto
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
            CreatedAt = p.JoinedDate
        });
    }

    public async Task<ParticipationDto?> GetParticipationAsync(int participationId)
    {
        var participation = await _participationRepository.GetByIdWithDetailsAsync(participationId);
        if (participation == null)
        {
            return null;
        }

        return new ParticipationDto
        {
            ParticipationId = participation.ParticipationId,
            ChallengeId = participation.ChallengeId,
            UserId = participation.UserId,
            JoinedDate = participation.JoinedDate,
            Status = participation.Status,
            SubmissionText = participation.SubmissionText,
            SubmissionUrl = participation.SubmissionUrl,
            SubmittedAt = participation.SubmittedAt,
            ReviewedByAdminId = participation.ReviewedByAdminId,
            ReviewDate = participation.ReviewDate,
            ReviewNotes = participation.ReviewNotes,
            CompletedAt = participation.CompletedAt,
            CreatedAt = participation.JoinedDate
        };
    }
}