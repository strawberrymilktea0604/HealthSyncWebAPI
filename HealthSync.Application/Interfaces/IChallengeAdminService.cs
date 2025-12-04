using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.DTOs;

namespace HealthSync.Application.Interfaces;

public interface IChallengeAdminService
{
    /// <summary>
    /// Create new challenge
    /// </summary>
    Task<(bool Success, ChallengeDto? Data, string Message)> CreateChallengeAsync(CreateChallengeRequest request, int adminId);

    /// <summary>
    /// Get challenge by ID
    /// </summary>
    Task<(bool Success, ChallengeDto? Data, string Message)> GetChallengeAsync(int challengeId);

    /// <summary>
    /// Get all challenges with pagination
    /// </summary>
    Task<(bool Success, PaginatedResult<ChallengeDto>? Data, string Message)> GetAllChallengesAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Update challenge
    /// </summary>
    Task<(bool Success, ChallengeDto? Data, string Message)> UpdateChallengeAsync(int challengeId, UpdateChallengeRequest request, int adminId);

    /// <summary>
    /// Delete challenge
    /// </summary>
    Task<(bool Success, string Message)> DeleteChallengeAsync(int challengeId, int adminId);

    /// <summary>
    /// Get pending approvals for a challenge
    /// </summary>
    Task<(bool Success, List<ParticipationDto>? Data, string Message)> GetPendingApprovalsAsync(int challengeId);

    /// <summary>
    /// Review participation submission (approve/reject)
    /// </summary>
    Task<(bool Success, ParticipationDto? Data, string Message)> ReviewParticipationAsync(
        int participationId, 
        ReviewParticipationRequest request, 
        int adminId);

    /// <summary>
    /// Get all participants for a challenge
    /// </summary>
    Task<(bool Success, List<ParticipationDto>? Data, string Message)> GetChallengeParticipantsAsync(int challengeId);
}
