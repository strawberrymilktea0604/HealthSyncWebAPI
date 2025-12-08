using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

/// <summary>
/// Repository for ExerciseSession entity
/// </summary>
public class ExerciseSessionRepository : IExerciseSessionRepository
{
    private readonly ApplicationDbContext _context;

    public ExerciseSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all exercise sessions
    /// </summary>
    public async Task<IEnumerable<ExerciseSession>> GetAllAsync()
    {
        return await _context.ExerciseSessions
            .Include(es => es.Exercise)
            .Include(es => es.WorkoutLog)
            .ToListAsync();
    }

    /// <summary>
    /// Get exercise session by ID
    /// </summary>
    public async Task<ExerciseSession?> GetByIdAsync(int sessionId)
    {
        return await _context.ExerciseSessions
            .Include(es => es.Exercise)
            .Include(es => es.WorkoutLog)
            .FirstOrDefaultAsync(es => es.ExerciseSessionId == sessionId);
    }

    /// <summary>
    /// Get exercise sessions by workout ID
    /// </summary>
    public async Task<IEnumerable<ExerciseSession>> GetByWorkoutIdAsync(int workoutLogId)
    {
        return await _context.ExerciseSessions
            .Where(es => es.WorkoutLogId == workoutLogId)
            .Include(es => es.Exercise)
            .ToListAsync();
    }

    /// <summary>
    /// Get exercise sessions by exercise ID
    /// </summary>
    public async Task<IEnumerable<ExerciseSession>> GetByExerciseIdAsync(int exerciseId)
    {
        return await _context.ExerciseSessions
            .Where(es => es.ExerciseId == exerciseId)
            .Include(es => es.WorkoutLog)
            .ToListAsync();
    }

    /// <summary>
    /// Add new exercise session
    /// </summary>
    public async Task<ExerciseSession> AddAsync(ExerciseSession session)
    {
        await _context.ExerciseSessions.AddAsync(session);
        await _context.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Update exercise session
    /// </summary>
    public async Task UpdateAsync(ExerciseSession session)
    {
        _context.ExerciseSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Delete exercise session
    /// </summary>
    public async Task DeleteAsync(int sessionId)
    {
        var session = await _context.ExerciseSessions.FindAsync(sessionId);
        if (session != null)
        {
            _context.ExerciseSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Save changes to database
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
