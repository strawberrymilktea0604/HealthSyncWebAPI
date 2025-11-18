using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class WorkoutLogService : IWorkoutLogService
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly IExerciseRepository _exerciseRepository;

    public WorkoutLogService(IWorkoutLogRepository workoutLogRepository, IExerciseRepository exerciseRepository)
    {
        _workoutLogRepository = workoutLogRepository;
        _exerciseRepository = exerciseRepository;
    }

    public async Task<WorkoutLogResponse> CreateWorkoutLogAsync(int userId, CreateWorkoutLogRequest request)
    {
        // Verify all exercises exist
        var exerciseIds = request.ExerciseSessions.Select(es => es.ExerciseId).Distinct().ToList();
        var exercises = new Dictionary<int, Exercise>();
        
        foreach (var id in exerciseIds)
        {
            var exercise = await _exerciseRepository.GetByIdAsync(id);
            if (exercise == null)
            {
                throw new ArgumentException($"Exercise ID {id} not found");
            }
            exercises[id] = exercise;
        }

        // Create WorkoutLog with ExerciseSessions
        var workoutLog = new WorkoutLog
        {
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            TotalDurationMinutes = request.TotalDurationMinutes,
            EstimatedCaloriesBurned = request.EstimatedCaloriesBurned,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var sessions = request.ExerciseSessions.Select(es => new ExerciseSession
        {
            ExerciseId = es.ExerciseId,
            Sets = es.Sets,
            Reps = es.Reps,
            WeightKg = es.WeightKg,
            RestSeconds = es.RestSeconds,
            Rpe = es.Rpe,
            DurationMinutes = es.DurationMinutes,
            Notes = es.Notes,
            OrderIndex = es.OrderIndex
        }).ToList();

        var createdLog = await _workoutLogRepository.CreateWithSessionsAsync(workoutLog, sessions);

        // Return response
        return new WorkoutLogResponse
        {
            WorkoutLogId = createdLog.WorkoutLogId,
            UserId = createdLog.UserId,
            WorkoutDate = createdLog.WorkoutDate,
            TotalDurationMinutes = createdLog.TotalDurationMinutes,
            EstimatedCaloriesBurned = createdLog.EstimatedCaloriesBurned,
            Notes = createdLog.Notes,
            CreatedAt = createdLog.CreatedAt,
            ExerciseSessions = createdLog.ExerciseSessions.Select(s => new ExerciseSessionDto
            {
                ExerciseSessionId = s.ExerciseSessionId,
                ExerciseId = s.ExerciseId,
                Sets = s.Sets,
                Reps = s.Reps,
                WeightKg = s.WeightKg,
                RestSeconds = s.RestSeconds,
                Rpe = s.Rpe,
                DurationMinutes = s.DurationMinutes,
                Notes = s.Notes,
                OrderIndex = s.OrderIndex
            }).ToList()
        };
    }

    public async Task<PaginatedResult<WorkoutLogResponse>> GetWorkoutLogsAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        var result = await _workoutLogRepository.GetByUserIdAsync(userId, pageNumber, pageSize);

        var items = result.Items.Select(wl => new WorkoutLogResponse
        {
            WorkoutLogId = wl.WorkoutLogId,
            UserId = wl.UserId,
            WorkoutDate = wl.WorkoutDate,
            TotalDurationMinutes = wl.TotalDurationMinutes,
            EstimatedCaloriesBurned = wl.EstimatedCaloriesBurned,
            Notes = wl.Notes,
            CreatedAt = wl.CreatedAt,
            ExerciseSessions = wl.ExerciseSessions
                .OrderBy(es => es.OrderIndex)
                .Select(es => new ExerciseSessionDto
                {
                    ExerciseSessionId = es.ExerciseSessionId,
                    ExerciseId = es.ExerciseId,
                    Sets = es.Sets,
                    Reps = es.Reps,
                    WeightKg = es.WeightKg,
                    RestSeconds = es.RestSeconds,
                    Rpe = es.Rpe,
                    DurationMinutes = es.DurationMinutes,
                    Notes = es.Notes,
                    OrderIndex = es.OrderIndex
                }).ToList()
        }).ToList();

        return new PaginatedResult<WorkoutLogResponse>(items, result.TotalItems, result.CurrentPage, result.PageSize);
    }
}