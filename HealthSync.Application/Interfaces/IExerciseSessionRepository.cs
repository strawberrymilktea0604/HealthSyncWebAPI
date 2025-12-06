using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

/// <summary>
/// Repository interface for ExerciseSession
/// </summary>
public interface IExerciseSessionRepository
{
    /// <summary>
    /// Get all exercise sessions
    /// </summary>
    Task<IEnumerable<ExerciseSession>> GetAllAsync();

    /// <summary>
    /// Get exercise sessions by ID with related data
    /// </summary>
    Task<ExerciseSession?> GetByIdAsync(int sessionId);

    /// <summary>
    /// Get exercise sessions by workout ID
    /// </summary>
    Task<IEnumerable<ExerciseSession>> GetByWorkoutIdAsync(int workoutLogId);

    /// <summary>
    /// Get exercise sessions by exercise ID
    /// </summary>
    Task<IEnumerable<ExerciseSession>> GetByExerciseIdAsync(int exerciseId);

    /// <summary>
    /// Add new exercise session
    /// </summary>
    Task<ExerciseSession> AddAsync(ExerciseSession session);

    /// <summary>
    /// Update exercise session
    /// </summary>
    Task UpdateAsync(ExerciseSession session);

    /// <summary>
    /// Delete exercise session
    /// </summary>
    Task DeleteAsync(int sessionId);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}
