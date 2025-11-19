using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IWorkoutLogRepository
{
    Task<WorkoutLog> AddAsync(WorkoutLog workoutLog);
    Task<ExerciseSession> AddExerciseSessionAsync(ExerciseSession session);
    Task<IEnumerable<ExerciseSession>> GetExerciseSessionsAsync(int workoutLogId);
    Task UpdateAsync(WorkoutLog workoutLog);
    Task<WorkoutLog> CreateWithSessionsAsync(WorkoutLog workoutLog, List<ExerciseSession> sessions);
    Task<PaginatedResult<WorkoutLog>> GetByUserIdAsync(int userId, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null);
    Task<WorkoutLog?> GetByIdAsync(int workoutLogId);
}