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

    public async Task<PaginatedResult<WorkoutLog>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _context.WorkoutLogs
            .Where(wl => wl.UserId == userId)
            .OrderByDescending(wl => wl.WorkoutDate)
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
}