using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IChallengeRepository
{
    /// <summary>
    /// Get challenge by ID with participations
    /// </summary>
    Task<Challenge?> GetByIdWithParticipationsAsync(int challengeId);

    /// <summary>
    /// Get challenge by ID
    /// </summary>
    Task<Challenge?> GetByIdAsync(int challengeId);

    /// <summary>
    /// Get all challenges with pagination
    /// </summary>
    Task<(List<Challenge> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Get challenges by status
    /// </summary>
    Task<List<Challenge>> GetByStatusAsync(ChallengeStatus status);

    /// <summary>
    /// Add new challenge
    /// </summary>
    Task<Challenge> AddAsync(Challenge challenge);

    /// <summary>
    /// Update challenge
    /// </summary>
    Task UpdateAsync(Challenge challenge);

    /// <summary>
    /// Delete challenge
    /// </summary>
    Task DeleteAsync(int challengeId);

    /// <summary>
    /// Check if challenge exists
    /// </summary>
    Task<bool> ExistsAsync(int challengeId);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}
