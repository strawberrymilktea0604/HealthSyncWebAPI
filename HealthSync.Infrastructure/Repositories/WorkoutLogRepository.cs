using HealthSync.Application.DTOs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class WorkoutLogRepository : IWorkoutLogRepository
{
    private readonly ApplicationDbContext _context;

    public WorkoutLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutLog> CreateWithSessionsAsync(WorkoutLog workoutLog, List<ExerciseSession> sessions)
    {
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();

        foreach (var session in sessions)
        {
            session.WorkoutLogId = workoutLog.WorkoutLogId;
        }

        _context.ExerciseSessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        workoutLog.ExerciseSessions = sessions;
        return workoutLog;
    }

    public async Task<PaginatedResult<WorkoutLog>> GetByUserIdAsync(int userId, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.WorkoutLogs
            .Where(wl => wl.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(wl => wl.WorkoutDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(wl => wl.WorkoutDate <= endDate.Value);
        }

        query = query.OrderByDescending(wl => wl.WorkoutDate)
                     .ThenByDescending(wl => wl.CreatedAt);

        var totalItems = await query.CountAsync();

        var items = await query
            .Include(wl => wl.ExerciseSessions)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<WorkoutLog>(items, totalItems, pageNumber, pageSize);
    }

    public async Task<WorkoutLog?> GetByIdAsync(int workoutLogId)
    {
        return await _context.WorkoutLogs
            .Include(wl => wl.ExerciseSessions)
            .FirstOrDefaultAsync(wl => wl.WorkoutLogId == workoutLogId);
    }

    public async Task<WorkoutLog> AddAsync(WorkoutLog workoutLog)
    {
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();
        return workoutLog;
    }

    public async Task<ExerciseSession> AddExerciseSessionAsync(ExerciseSession session)
    {
        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<IEnumerable<ExerciseSession>> GetExerciseSessionsAsync(int workoutLogId)
    {
        return await _context.ExerciseSessions
            .Where(es => es.WorkoutLogId == workoutLogId)
            .ToListAsync();
    }

    public async Task UpdateAsync(WorkoutLog workoutLog)
    {
        _context.WorkoutLogs.Update(workoutLog);
        await _context.SaveChangesAsync();
    }
}