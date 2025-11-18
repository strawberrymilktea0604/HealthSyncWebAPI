using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IWorkoutLogRepository
{
    Task<WorkoutLog> CreateWithSessionsAsync(WorkoutLog workoutLog, List<ExerciseSession> sessions);
    Task<PaginatedResult<WorkoutLog>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
    Task<WorkoutLog?> GetByIdAsync(int workoutLogId);
}