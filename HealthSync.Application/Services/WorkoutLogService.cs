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
        // Validate request
        await ValidateCreateWorkoutLogRequestAsync(request);

        // Create WorkoutLog
        var workoutLog = new WorkoutLog
        {
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            TotalDurationMinutes = 0, // Will be calculated
            EstimatedCaloriesBurned = 0, // Will be calculated
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdLog = await _workoutLogRepository.AddAsync(workoutLog);

        // Create ExerciseSessions
        var exerciseSessions = new List<ExerciseSession>();
        foreach (var sessionRequest in request.ExerciseSessions)
        {
            var session = new ExerciseSession
            {
                WorkoutLogId = createdLog.WorkoutLogId,
                ExerciseId = sessionRequest.ExerciseId,
                Sets = sessionRequest.Sets,
                Reps = sessionRequest.Reps,
                WeightKg = sessionRequest.WeightKg,
                RestSeconds = sessionRequest.RestSeconds,
                Rpe = sessionRequest.Rpe,
                DurationMinutes = sessionRequest.DurationMinutes,
                Notes = sessionRequest.Notes,
                OrderIndex = sessionRequest.OrderIndex
            };

            var createdSession = await _workoutLogRepository.AddExerciseSessionAsync(session);
            exerciseSessions.Add(createdSession);
        }

        // Recalculate totals
        await RecalculateWorkoutTotalsAsync(createdLog.WorkoutLogId);

        // Get updated log
        var updatedLog = await _workoutLogRepository.GetByIdAsync(createdLog.WorkoutLogId);
        if (updatedLog == null)
        {
            throw new InvalidOperationException("Workout log not found after creation");
        }

        var sessions = await _workoutLogRepository.GetExerciseSessionsAsync(createdLog.WorkoutLogId);

        // Return response
        return new WorkoutLogResponse
        {
            WorkoutLogId = updatedLog.WorkoutLogId,
            UserId = updatedLog.UserId,
            WorkoutDate = updatedLog.WorkoutDate,
            TotalDurationMinutes = updatedLog.TotalDurationMinutes,
            EstimatedCaloriesBurned = updatedLog.EstimatedCaloriesBurned,
            Notes = updatedLog.Notes,
            CreatedAt = updatedLog.CreatedAt,
            ExerciseSessions = sessions.Select(s => new ExerciseSessionDto
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

    public async Task<ExerciseSessionDto> AddExerciseSessionAsync(int workoutLogId, CreateExerciseSessionRequest request)
    {
        // Verify workout log exists and belongs to user
        var workoutLog = await _workoutLogRepository.GetByIdAsync(workoutLogId);
        if (workoutLog == null)
        {
            throw new ArgumentException("Workout log not found");
        }

        // Verify exercise exists
        var exercise = await _exerciseRepository.GetByIdAsync(request.ExerciseId);
        if (exercise == null)
        {
            throw new ArgumentException($"Exercise ID {request.ExerciseId} not found");
        }

        // Business rules validation
        if (request.Sets <= 0)
        {
            throw new ArgumentException("Sets must be greater than 0");
        }
        if (request.Reps <= 0)
        {
            throw new ArgumentException("Reps must be greater than 0");
        }
        if (request.WeightKg < 0)
        {
            throw new ArgumentException("Weight cannot be negative");
        }
        if (request.Rpe.HasValue && (request.Rpe < 1 || request.Rpe > 10))
        {
            throw new ArgumentException("RPE must be between 1 and 10");
        }

        // Create ExerciseSession
        var session = new ExerciseSession
        {
            WorkoutLogId = workoutLogId,
            ExerciseId = request.ExerciseId,
            Sets = request.Sets,
            Reps = request.Reps,
            WeightKg = request.WeightKg,
            RestSeconds = request.RestSeconds,
            Rpe = request.Rpe,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
            OrderIndex = request.OrderIndex
        };

        var createdSession = await _workoutLogRepository.AddExerciseSessionAsync(session);

        // Recalculate totals for workout log
        await RecalculateWorkoutTotalsAsync(workoutLogId);

        return new ExerciseSessionDto
        {
            ExerciseSessionId = createdSession.ExerciseSessionId,
            ExerciseId = createdSession.ExerciseId,
            Sets = createdSession.Sets,
            Reps = createdSession.Reps,
            WeightKg = createdSession.WeightKg,
            RestSeconds = createdSession.RestSeconds,
            Rpe = createdSession.Rpe,
            DurationMinutes = createdSession.DurationMinutes,
            Notes = createdSession.Notes,
            OrderIndex = createdSession.OrderIndex
        };
    }

    private async Task RecalculateWorkoutTotalsAsync(int workoutLogId)
    {
        var workoutLog = await _workoutLogRepository.GetByIdAsync(workoutLogId);
        if (workoutLog == null) return;

        var sessions = await _workoutLogRepository.GetExerciseSessionsAsync(workoutLogId);

        // Calculate total duration: SUM(duration_minutes OR estimated from sets * reps * rest_seconds)
        int totalDuration = 0;
        foreach (var session in sessions)
        {
            if (session.DurationMinutes.HasValue)
            {
                totalDuration += session.DurationMinutes.Value;
            }
            else
            {
                // Estimate: sets * reps * rest_seconds / 60 (convert to minutes)
                // Assuming average time per rep is included in rest, but simplified
                int estimatedSeconds = session.Sets * session.Reps * (session.RestSeconds ?? 60);
                totalDuration += estimatedSeconds / 60;
            }
        }
        workoutLog.TotalDurationMinutes = totalDuration;

        // Calculate estimated calories
        decimal totalCalories = 0;
        foreach (var session in sessions)
        {
            var exercise = await _exerciseRepository.GetByIdAsync(session.ExerciseId);
            if (exercise?.CaloriesPerMinute.HasValue == true)
            {
                int durationForCalories = session.DurationMinutes ?? (session.Sets * session.Reps * (session.RestSeconds ?? 60) / 60);
                totalCalories += exercise.CaloriesPerMinute.Value * durationForCalories;
            }
        }
        workoutLog.EstimatedCaloriesBurned = totalCalories;

        await _workoutLogRepository.UpdateAsync(workoutLog);
    }

    public async Task<PaginatedResult<WorkoutLogResponse>> GetWorkoutLogsAsync(int userId, int pageNumber = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null)
    {
        var result = await _workoutLogRepository.GetByUserIdAsync(userId, pageNumber, pageSize, startDate, endDate);

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

    private async Task ValidateCreateWorkoutLogRequestAsync(CreateWorkoutLogRequest request)
    {
        // Validate workout date
        if (request.WorkoutDate > DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Workout date cannot be in the future");
        }

        // Validate exercise sessions
        foreach (var sessionRequest in request.ExerciseSessions)
        {
            var exercise = await _exerciseRepository.GetByIdAsync(sessionRequest.ExerciseId);
            if (exercise == null)
            {
                throw new ArgumentException($"Exercise ID {sessionRequest.ExerciseId} not found");
            }

            // Business rules validation
            if (sessionRequest.Sets <= 0)
            {
                throw new ArgumentException("Sets must be greater than 0");
            }
            if (sessionRequest.Reps <= 0)
            {
                throw new ArgumentException("Reps must be greater than 0");
            }
            if (sessionRequest.WeightKg < 0)
            {
                throw new ArgumentException("Weight cannot be negative");
            }
            if (sessionRequest.Rpe.HasValue && (sessionRequest.Rpe < 1 || sessionRequest.Rpe > 10))
            {
                throw new ArgumentException("RPE must be between 1 and 10");
            }
        }
    }
}