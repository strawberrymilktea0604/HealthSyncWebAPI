using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IChallengeParticipationRepository
{
    /// <summary>
    /// Get participation by ID with related data
    /// </summary>
    Task<ChallengeParticipation?> GetByIdWithDetailsAsync(int participationId);

    /// <summary>
    /// Get participation by ID
    /// </summary>
    Task<ChallengeParticipation?> GetByIdAsync(int participationId);

    /// <summary>
    /// Get all participations for a challenge
    /// </summary>
    Task<List<ChallengeParticipation>> GetByChallengeIdAsync(int challengeId);

    /// <summary>
    /// Get participations by status for a challenge
    /// </summary>
    Task<List<ChallengeParticipation>> GetByChallengeAndStatusAsync(int challengeId, ParticipationStatus status);

    /// <summary>
    /// Get user's participation in a challenge
    /// </summary>
    Task<ChallengeParticipation?> GetUserParticipationAsync(int challengeId, int userId);

    /// <summary>
    /// Check if user already participated
    /// </summary>
    Task<bool> IsUserParticipatedAsync(int challengeId, int userId);

    /// <summary>
    /// Get total participants count for a challenge
    /// </summary>
    Task<int> GetParticipantCountAsync(int challengeId);

    /// <summary>
    /// Get pending approvals count
    /// </summary>
    Task<int> GetPendingApprovalsCountAsync();

    /// <summary>
    /// Add new participation
    /// </summary>
    Task<ChallengeParticipation> AddAsync(ChallengeParticipation participation);

    /// <summary>
    /// Update participation
    /// </summary>
    Task UpdateAsync(ChallengeParticipation participation);

    /// <summary>
    /// Delete participation
    /// </summary>
    Task DeleteAsync(int participationId);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}
