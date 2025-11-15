using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.DTOs;

namespace HealthSync.Application.Interfaces;

public interface IWorkoutLogService
{
    Task<WorkoutLogResponse> CreateWorkoutLogAsync(int userId, CreateWorkoutLogRequest request);
    Task<PaginatedResult<WorkoutLogResponse>> GetWorkoutLogsAsync(int userId, int pageNumber = 1, int pageSize = 20);
}