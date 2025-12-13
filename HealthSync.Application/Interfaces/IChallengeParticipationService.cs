using HealthSync.Application.DTOs.Challenges;

namespace HealthSync.Application.Interfaces;

public interface IChallengeParticipationService
{
    /// <summary>
    /// Join a challenge
    /// </summary>
    Task<ParticipationDto> JoinChallengeAsync(int challengeId, int userId);

    /// <summary>
    /// Submit challenge result
    /// </summary>
    Task<ParticipationDto> SubmitChallengeResultAsync(int challengeId, int userId, SubmitChallengeRequest request);

    /// <summary>
    /// Get user's participations
    /// </summary>
    Task<IEnumerable<ParticipationDto>> GetUserParticipationsAsync(int userId);

    /// <summary>
    /// Get participation details
    /// </summary>
    Task<ParticipationDto?> GetParticipationAsync(int participationId);
}