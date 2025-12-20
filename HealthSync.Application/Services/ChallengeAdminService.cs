using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.DTOs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class ChallengeAdminService : IChallengeAdminService
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeParticipationRepository _participationRepository;
    private readonly IUserRepository _userRepository;

    public ChallengeAdminService(
        IChallengeRepository challengeRepository,
        IChallengeParticipationRepository participationRepository,
        IUserRepository userRepository)
    {
        _challengeRepository = challengeRepository;
        _participationRepository = participationRepository;
        _userRepository = userRepository;
    }

    public async Task<(bool Success, ChallengeDto? Data, string Message)> CreateChallengeAsync(CreateChallengeRequest request, int adminId)
    {
        try
        {
            // Validate dates
            if (request.EndDate <= request.StartDate)
                return (false, null, "End date must be after start date");

            if (request.StartDate < DateTime.UtcNow)
                return (false, null, "Start date cannot be in the past");

            // Check admin exists
            var admin = await _userRepository.GetByIdAsync(adminId);
            if (admin is null)
                return (false, null, "Admin not found");

            var challenge = new Challenge
            {
                Title = request.Title,
                Description = request.Description,
                ChallengeType = request.ChallengeType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Criteria = request.Criteria,
                MaxParticipants = request.MaxParticipants,
                RewardDescription = request.RewardDescription,
                Status = ChallengeStatus.Open,
                CreatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _challengeRepository.AddAsync(challenge);
            await _challengeRepository.SaveChangesAsync();

            return (true, MapToDto(challenge, 0), "Challenge created successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error creating challenge: {ex.Message}");
        }
    }

    public async Task<(bool Success, ChallengeDto? Data, string Message)> GetChallengeAsync(int challengeId)
    {
        try
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge is null)
                return (false, null, "Challenge not found");

            var participantCount = await _participationRepository.GetParticipantCountAsync(challengeId);
            return (true, MapToDto(challenge, participantCount), "Challenge retrieved successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error retrieving challenge: {ex.Message}");
        }
    }

    public async Task<(bool Success, PaginatedResult<ChallengeDto>? Data, string Message)> GetAllChallengesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (challenges, totalCount) = await _challengeRepository.GetAllAsync(page, pageSize);

            var dtos = new List<ChallengeDto>();
            foreach (var challenge in challenges)
            {
                var participantCount = await _participationRepository.GetParticipantCountAsync(challenge.ChallengeId);
                dtos.Add(MapToDto(challenge, participantCount));
            }

            var result = new PaginatedResult<ChallengeDto>
            {
                Items = dtos,
                TotalItems = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return (true, result, "Challenges retrieved successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error retrieving challenges: {ex.Message}");
        }
    }

    public async Task<(bool Success, ChallengeDto? Data, string Message)> UpdateChallengeAsync(
        int challengeId, 
        UpdateChallengeRequest request, 
        int adminId)
    {
        try
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge is null)
                return (false, null, "Challenge not found");

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Title))
                challenge.Title = request.Title;

            if (!string.IsNullOrEmpty(request.Description))
                challenge.Description = request.Description;

            if (request.Status.HasValue)
                challenge.Status = request.Status.Value;

            if (request.MaxParticipants.HasValue)
                challenge.MaxParticipants = request.MaxParticipants.Value;

            if (!string.IsNullOrEmpty(request.RewardDescription))
                challenge.RewardDescription = request.RewardDescription;

            challenge.UpdatedAt = DateTime.UtcNow;

            await _challengeRepository.UpdateAsync(challenge);
            await _challengeRepository.SaveChangesAsync();

            var participantCount = await _participationRepository.GetParticipantCountAsync(challengeId);
            return (true, MapToDto(challenge, participantCount), "Challenge updated successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error updating challenge: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteChallengeAsync(int challengeId, int adminId)
    {
        try
        {
            var exists = await _challengeRepository.ExistsAsync(challengeId);
            if (!exists)
                return (false, "Challenge not found");

            await _challengeRepository.DeleteAsync(challengeId);
            await _challengeRepository.SaveChangesAsync();

            return (true, "Challenge deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error deleting challenge: {ex.Message}");
        }
    }

    public async Task<(bool Success, List<ParticipationDto>? Data, string Message)> GetPendingApprovalsAsync(int challengeId)
    {
        try
        {
            var participations = await _participationRepository.GetByChallengeAndStatusAsync(
                challengeId, 
                ParticipationStatus.PendingApproval);

            var dtos = participations.Select(p => MapParticipationToDto(p)).ToList();
            return (true, dtos, "Pending approvals retrieved successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error retrieving pending approvals: {ex.Message}");
        }
    }

    public async Task<(bool Success, ParticipationDto? Data, string Message)> ReviewParticipationAsync(
        int participationId, 
        ReviewParticipationRequest request, 
        int adminId)
    {
        try
        {
            var participation = await _participationRepository.GetByIdWithDetailsAsync(participationId);
            if (participation is null)
                return (false, null, "Participation not found");

            if (participation.Status != ParticipationStatus.PendingApproval)
                return (false, null, "Participation is not pending approval");

            // Update participation status
            participation.Status = request.Approved ? ParticipationStatus.Completed : ParticipationStatus.Failed;
            participation.ReviewedByAdminId = adminId;
            participation.ReviewDate = DateTime.UtcNow;
            participation.ReviewNotes = request.ReviewNotes;

            if (request.Approved)
                participation.CompletedAt = DateTime.UtcNow;

            await _participationRepository.UpdateAsync(participation);
            await _participationRepository.SaveChangesAsync();

            return (true, MapParticipationToDto(participation), 
                request.Approved ? "Participation approved successfully" : "Participation rejected successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error reviewing participation: {ex.Message}");
        }
    }

    public async Task<(bool Success, List<ParticipationDto>? Data, string Message)> GetChallengeParticipantsAsync(int challengeId)
    {
        try
        {
            var participations = await _participationRepository.GetByChallengeIdAsync(challengeId);
            var dtos = participations.Select(p => MapParticipationToDto(p)).ToList();
            return (true, dtos, "Participants retrieved successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error retrieving participants: {ex.Message}");
        }
    }

    public async Task<(bool Success, PaginatedResult<ParticipationDto>? Data, string Message)> GetAllPendingApprovalsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (participations, totalCount) = await _participationRepository.GetAllPendingApprovalsAsync(page, pageSize);

            var dtos = participations.Select(p => MapParticipationToDto(p)).ToList();

            var result = new PaginatedResult<ParticipationDto>(dtos, totalCount, page, pageSize);

            return (true, result, "Pending approvals retrieved successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error retrieving pending approvals: {ex.Message}");
        }
    }

    // Helper methods
    private static ChallengeDto MapToDto(Challenge challenge, int participantCount)
    {
        return new ChallengeDto
        {
            ChallengeId = challenge.ChallengeId,
            Title = challenge.Title,
            Description = challenge.Description,
            ChallengeType = challenge.ChallengeType,
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            Criteria = challenge.Criteria,
            Status = challenge.Status,
            MaxParticipants = challenge.MaxParticipants,
            CurrentParticipants = participantCount,
            RewardDescription = challenge.RewardDescription,
            ImageUrl = challenge.ImageUrl,
            CreatedByAdminId = challenge.CreatedByAdminId,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt
        };
    }

    private static ParticipationDto MapParticipationToDto(ChallengeParticipation participation)
    {
        return new ParticipationDto
        {
            ParticipationId = participation.ParticipationId,
            ChallengeId = participation.ChallengeId,
            UserId = participation.UserId,
            UserFullName = participation.User?.UserProfile?.FullName,
            JoinedDate = participation.JoinedDate,
            Status = participation.Status,
            SubmissionText = participation.SubmissionText,
            SubmissionUrl = participation.SubmissionUrl,
            SubmittedAt = participation.SubmittedAt,
            ReviewedByAdminId = participation.ReviewedByAdminId,
            ReviewDate = participation.ReviewDate,
            ReviewNotes = participation.ReviewNotes,
            CompletedAt = participation.CompletedAt
        };
    }

    public async Task<(bool Success, ParticipationDto? Data, string Message)> RejectParticipationAsync(
        int participationId,
        ReviewParticipationRequest request,
        int adminId)
    {
        try
        {
            // Get participation
            var participation = await _participationRepository.GetByIdWithDetailsAsync(participationId);
            if (participation is null)
                return (false, null, "Participation not found");

            // Check if status is PendingApproval
            if (participation.Status != ParticipationStatus.PendingApproval)
                return (false, null, $"Cannot reject participation with status '{participation.Status}'. Only 'PendingApproval' status can be rejected.");

            // Check admin exists
            var admin = await _userRepository.GetByIdAsync(adminId);
            if (admin is null)
                return (false, null, "Admin not found");

            // Update participation status to Failed
            participation.Status = ParticipationStatus.Failed;
            participation.ReviewedByAdminId = adminId;
            participation.ReviewDate = DateTime.UtcNow;
            participation.ReviewNotes = request.ReviewNotes;

            await _participationRepository.UpdateAsync(participation);
            await _participationRepository.SaveChangesAsync();

            return (true, MapParticipationToDto(participation), "Participation rejected successfully");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error rejecting participation: {ex.Message}");
        }
    }
}
