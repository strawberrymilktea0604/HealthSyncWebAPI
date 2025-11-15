using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.WorkoutLogs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Services;

public class WorkoutLogService : IWorkoutLogService
{
    private readonly ApplicationDbContext _context;

    public WorkoutLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutLogResponse> CreateWorkoutLogAsync(int userId, CreateWorkoutLogRequest request)
    {
        // Verify all exercises exist
        var exerciseIds = request.ExerciseSessions.Select(es => es.ExerciseId).Distinct().ToList();
        var exercises = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.ExerciseId))
            .ToDictionaryAsync(e => e.ExerciseId);

        var missingIds = exerciseIds.Except(exercises.Keys).ToList();
        if (missingIds.Any())
        {
            throw new ArgumentException($"Exercise IDs not found: {string.Join(", ", missingIds)}");
        }

        // Create WorkoutLog
        var workoutLog = new WorkoutLog
        {
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            TotalDurationMinutes = request.TotalDurationMinutes,
            EstimatedCaloriesBurned = request.EstimatedCaloriesBurned,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        // Create ExerciseSessions
        var sessions = request.ExerciseSessions.Select(es => new ExerciseSession
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
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

        _context.ExerciseSessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Return response
        var response = new WorkoutLogResponse
        {
            WorkoutLogId = workoutLog.WorkoutLogId,
            UserId = workoutLog.UserId,
            WorkoutDate = workoutLog.WorkoutDate,
            TotalDurationMinutes = workoutLog.TotalDurationMinutes,
            EstimatedCaloriesBurned = workoutLog.EstimatedCaloriesBurned,
            Notes = workoutLog.Notes,
            CreatedAt = workoutLog.CreatedAt,
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

        return response;
    }

    public async Task<PaginatedResult<WorkoutLogResponse>> GetWorkoutLogsAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        var query = _context.WorkoutLogs
            .Where(wl => wl.UserId == userId)
            .OrderByDescending(wl => wl.WorkoutDate)
            .ThenByDescending(wl => wl.CreatedAt)
            .AsQueryable();

        var totalItems = await query.CountAsync();

        var workoutLogs = await query
            .Include(wl => wl.ExerciseSessions)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = workoutLogs.Select(wl => new WorkoutLogResponse
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

        return new PaginatedResult<WorkoutLogResponse>(items, totalItems, pageNumber, pageSize);
    }
}